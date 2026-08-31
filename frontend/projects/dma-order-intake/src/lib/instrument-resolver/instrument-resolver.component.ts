import { NgTemplateOutlet } from '@angular/common';
import { Component, computed, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { OrderIntakeApiService } from '../order-intake-api.service';
import { InstrumentMatch, InstrumentResolutionResult } from '../models/instrument.model';
import { OrderDto } from '../models/order.model';

// Real functionality: "which instrument, exactly" has to be answered — and
// explicitly confirmed — before an order can be submitted. Resolve and
// Confirm are always two separate actions; a match is never auto-selected,
// even when there's only one.
//
// Two modes:
// - orderId set: Confirm calls the backend immediately (attaches the
//   instrument to that already-existing order).
// - orderId unset: Confirm only emits the chosen match locally — used inside
//   a wizard where the order doesn't exist yet. The caller persists the
//   choice itself once it does (via OrderIntakeApiService.confirmInstrument).
@Component({
  selector: 'doi-instrument-resolver',
  standalone: true,
  imports: [FormsModule, NgTemplateOutlet],
  templateUrl: './instrument-resolver.component.html',
  styleUrl: './instrument-resolver.component.scss',
})
export class InstrumentResolverComponent {
  private readonly api = inject(OrderIntakeApiService);

  readonly orderId = input<string | null>(null);

  // Fires whenever the customer confirms a candidate, in both modes.
  readonly instrumentSelected = output<InstrumentMatch>();
  // Fires only in "orderId set" mode, once the backend call succeeds.
  readonly instrumentConfirmed = output<OrderDto>();

  readonly isin = signal('');
  readonly mic = signal('');

  readonly resolving = signal(false);
  readonly result = signal<InstrumentResolutionResult | null>(null);
  readonly errorMessage = signal<string | null>(null);

  readonly confirming = signal<string | null>(null);
  readonly confirmedInstrumentId = signal<string | null>(null);

  // Which row the customer clicked in the MultipleMatches list — nothing is
  // ever pre-selected, and picking a row is still just a local UI choice,
  // not a confirm.
  readonly selectedMatchId = signal<string | null>(null);
  readonly selectedCandidate = computed(() => {
    const id = this.selectedMatchId();
    return id ? (this.result()?.matches.find((m) => m.instrumentId === id) ?? null) : null;
  });

  resolve(): void {
    if (!this.isin().trim()) {
      return;
    }

    this.resolving.set(true);
    this.errorMessage.set(null);
    this.result.set(null);
    this.confirmedInstrumentId.set(null);
    this.selectedMatchId.set(null);

    this.api.resolveInstrument({ isin: this.isin().trim(), mic: this.mic().trim() || null }).subscribe({
      next: (result) => {
        this.result.set(result);
        this.resolving.set(false);
      },
      error: (err: unknown) => {
        this.errorMessage.set(err instanceof Error ? err.message : 'Could not resolve instrument.');
        this.resolving.set(false);
      },
    });
  }

  confirm(match: InstrumentMatch): void {
    const orderId = this.orderId();

    if (!orderId) {
      // No order to attach to yet — just record the customer's explicit choice.
      this.confirmedInstrumentId.set(match.instrumentId);
      this.instrumentSelected.emit(match);
      return;
    }

    this.confirming.set(match.instrumentId);

    this.api
      .confirmInstrument(orderId, { instrumentId: match.instrumentId, figi: match.figi, instrumentName: match.name })
      .subscribe({
      next: (order) => {
        this.confirmedInstrumentId.set(match.instrumentId);
        this.confirming.set(null);
        this.instrumentSelected.emit(match);
        this.instrumentConfirmed.emit(order);
      },
      error: (err: unknown) => {
        this.errorMessage.set(err instanceof Error ? err.message : 'Could not confirm instrument.');
        this.confirming.set(null);
      },
    });
  }
}
