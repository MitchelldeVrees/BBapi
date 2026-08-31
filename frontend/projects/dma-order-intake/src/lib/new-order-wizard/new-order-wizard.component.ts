import { DecimalPipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { interval, switchMap, take, takeWhile, tap } from 'rxjs';
import { AccountSelectorComponent } from '../account-selector/account-selector.component';
import { InstrumentResolverComponent } from '../instrument-resolver/instrument-resolver.component';
import { CustomerAccount } from '../models/customer-account.model';
import { InstrumentMatch } from '../models/instrument.model';
import { CreateOrderRequest, OrderDto } from '../models/order.model';
import { OrderIntakeApiService } from '../order-intake-api.service';

type WizardStep = 'account' | 'instrument' | 'order-details' | 'review' | 'done';
type Side = 'Buy' | 'Sell';
type OrderTypeChoice = 'Market' | 'Limit';

const STAGING_IN_FLIGHT_STATUSES = new Set(['StagingPending', 'StagingInProgress']);

// The real business flow: five screens, nothing persisted until "Submit
// order" on the review screen — Account and Instrument are captured as local
// wizard state first (see InstrumentResolverComponent's no-orderId mode) and
// only turned into an actual Order + confirmed instrument + submission as one
// sequence at the very end, after the customer has reviewed everything.
@Component({
  selector: 'doi-new-order-wizard',
  standalone: true,
  imports: [FormsModule, DecimalPipe, AccountSelectorComponent, InstrumentResolverComponent],
  templateUrl: './new-order-wizard.component.html',
  styleUrl: './new-order-wizard.component.scss',
})
export class NewOrderWizardComponent {
  private readonly api = inject(OrderIntakeApiService);

  readonly step = signal<WizardStep>('account');

  readonly selectedAccount = signal<CustomerAccount | null>(null);
  readonly selectedInstrument = signal<InstrumentMatch | null>(null);

  readonly side = signal<Side>('Buy');
  readonly quantity = signal<number | null>(null);
  readonly orderType = signal<OrderTypeChoice>('Limit');
  readonly limitPrice = signal<number | null>(null);
  readonly customerReference = signal('');
  readonly notes = signal('');

  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly submittedOrder = signal<OrderDto | null>(null);

  // Populated once CreateOrder + ConfirmInstrument succeed. If the final
  // submit call itself then fails, "Retry submit" reuses both unchanged —
  // that's the concrete demonstration that a retried submit is safe, not a
  // second order: see OrderIntakeApiService.submitOrder / SubmitOrderHandler.
  private readonly pendingOrderId = signal<string | null>(null);
  private readonly pendingIdempotencyKey = signal<string | null>(null);

  readonly canRetrySubmit = computed(() => this.pendingOrderId() !== null && this.errorMessage() !== null);

  readonly orderDetailsValid = computed(() => {
    const quantity = this.quantity();
    if (quantity === null || quantity <= 0) {
      return false;
    }
    return this.orderType() === 'Market' || (this.limitPrice() !== null && this.limitPrice()! > 0);
  });

  readonly isStagingInFlight = computed(() => {
    const order = this.submittedOrder();
    return order !== null && STAGING_IN_FLIGHT_STATUSES.has(order.status);
  });

  onAccountSelected(account: CustomerAccount): void {
    this.selectedAccount.set(account);
  }

  onInstrumentSelected(match: InstrumentMatch): void {
    this.selectedInstrument.set(match);
  }

  goTo(step: WizardStep): void {
    this.step.set(step);
  }

  submitOrder(): void {
    const account = this.selectedAccount();
    const instrument = this.selectedInstrument();
    if (!account || !instrument || !this.orderDetailsValid()) {
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);

    const createRequest: CreateOrderRequest = {
      customerId: account.customerId,
      customerName: account.customerName,
      accountId: account.accountId,
      accountCode: account.accountCode,
      side: this.side(),
      quantity: this.quantity()!,
      orderType: this.orderType(),
      limitPrice: this.orderType() === 'Limit' ? this.limitPrice() : null,
      currency: instrument.currency,
      customerReference: this.customerReference().trim() || null,
      notes: this.notes().trim() || null,
    };

    this.api
      .createOrder(createRequest)
      .pipe(
        switchMap((order) =>
          this.api
            .confirmInstrument(order.id, {
              instrumentId: instrument.instrumentId,
              figi: instrument.figi,
              instrumentName: instrument.name,
            })
            .pipe(switchMap(() => [order.id])),
        ),
      )
      .subscribe({
        next: (orderId) => {
          this.pendingOrderId.set(orderId);
          this.pendingIdempotencyKey.set(crypto.randomUUID());
          this.doSubmit();
        },
        error: (err: unknown) => {
          this.errorMessage.set(err instanceof Error ? err.message : 'Could not create order.');
          this.submitting.set(false);
        },
      });
  }

  // Reuses the SAME orderId + idempotency key as the failed attempt — this is
  // what "Angular accidentally submits twice" looks like from the wizard's
  // side, and it's safe: SubmitOrderHandler treats the retry as a no-op, not
  // a second order.
  retrySubmit(): void {
    this.submitting.set(true);
    this.errorMessage.set(null);
    this.doSubmit();
  }

  private doSubmit(): void {
    const orderId = this.pendingOrderId();
    const idempotencyKey = this.pendingIdempotencyKey();
    if (!orderId || !idempotencyKey) {
      return;
    }

    this.api.submitOrder(orderId, idempotencyKey).subscribe({
      next: (order) => {
        this.submittedOrder.set(order);
        this.submitting.set(false);
        this.step.set('done');
        this.pollForStagingCompletion(order.id);
      },
      error: (err: unknown) => {
        this.errorMessage.set(err instanceof Error ? err.message : 'Could not submit order.');
        this.submitting.set(false);
      },
    });
  }

  startNewOrder(): void {
    this.step.set('account');
    this.selectedAccount.set(null);
    this.selectedInstrument.set(null);
    this.side.set('Buy');
    this.quantity.set(null);
    this.orderType.set('Limit');
    this.limitPrice.set(null);
    this.customerReference.set('');
    this.notes.set('');
    this.errorMessage.set(null);
    this.submittedOrder.set(null);
    this.pendingOrderId.set(null);
    this.pendingIdempotencyKey.set(null);
  }

  // The submit call only ever returns StagingPending — this just makes the
  // asynchronous StagingInProgress -> StagedInEmsx transition (driven by the
  // outbox worker, entirely separate from this HTTP call) visible without a
  // page reload. Purely cosmetic: it stops on its own once staging settles.
  private pollForStagingCompletion(orderId: string): void {
    interval(1500)
      .pipe(
        switchMap(() => this.api.getOrderById(orderId)),
        tap((order) => this.submittedOrder.set(order)),
        takeWhile((order) => STAGING_IN_FLIGHT_STATUSES.has(order.status), true),
        take(10),
      )
      .subscribe();
  }
}
