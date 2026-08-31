import { DecimalPipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuditEventDto } from '../models/audit-event.model';
import { OrderDto } from '../models/order.model';
import { OrderIntakeApiService } from '../order-intake-api.service';

type ViewMode = 'list' | 'detail';

// Real functionality: order-management, not demo scaffolding. Shows every
// order this backend knows about (structured order + status tracking +
// auditability + automated EMSX staging — the whole point of replacing "an
// email a DMA employee interprets" with this).
@Component({
  selector: 'doi-order-overview',
  standalone: true,
  imports: [FormsModule, DecimalPipe],
  templateUrl: './order-overview.component.html',
  styleUrl: './order-overview.component.scss',
})
export class OrderOverviewComponent implements OnInit {
  private readonly api = inject(OrderIntakeApiService);

  readonly view = signal<ViewMode>('list');
  readonly orders = signal<OrderDto[]>([]);
  readonly loading = signal(true);
  readonly errorMessage = signal<string | null>(null);

  readonly selectedOrder = signal<OrderDto | null>(null);
  readonly auditTrail = signal<AuditEventDto[]>([]);
  readonly auditLoading = signal(false);

  readonly statusFilter = signal('');
  readonly accountFilter = signal('');
  readonly instrumentFilter = signal('');
  readonly customerReferenceFilter = signal('');
  readonly dateFilter = signal('');

  readonly statusOptions = computed(() => [...new Set(this.orders().map((o) => o.status))].sort());
  readonly accountOptions = computed(() => [...new Set(this.orders().map((o) => o.accountCode))].sort());

  readonly filteredOrders = computed(() => {
    const status = this.statusFilter();
    const account = this.accountFilter();
    const instrument = this.instrumentFilter().trim().toLowerCase();
    const customerReference = this.customerReferenceFilter().trim().toLowerCase();
    const date = this.dateFilter();

    return this.orders().filter((order) => {
      if (status && order.status !== status) return false;
      if (account && order.accountCode !== account) return false;
      if (instrument && !(order.instrumentName ?? '').toLowerCase().includes(instrument)) return false;
      if (customerReference && !(order.customerReference ?? '').toLowerCase().includes(customerReference)) return false;
      if (date && !order.createdAtUtc.startsWith(date)) return false;
      return true;
    });
  });

  ngOnInit(): void {
    this.loadOrders();
  }

  loadOrders(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.api.getOrders().subscribe({
      next: (orders) => {
        this.orders.set(orders);
        this.loading.set(false);
      },
      error: (err: unknown) => {
        this.errorMessage.set(err instanceof Error ? err.message : 'Could not load orders.');
        this.loading.set(false);
      },
    });
  }

  clearFilters(): void {
    this.statusFilter.set('');
    this.accountFilter.set('');
    this.instrumentFilter.set('');
    this.customerReferenceFilter.set('');
    this.dateFilter.set('');
  }

  openOrder(order: OrderDto): void {
    this.selectedOrder.set(order);
    this.view.set('detail');
    this.auditLoading.set(true);
    this.auditTrail.set([]);

    this.api.getOrderAuditTrail(order.id).subscribe({
      next: (events) => {
        this.auditTrail.set(events);
        this.auditLoading.set(false);
      },
      error: () => {
        this.auditLoading.set(false);
      },
    });
  }

  backToList(): void {
    this.view.set('list');
    this.selectedOrder.set(null);
  }

  // Compact label for the overview table.
  displayStatus(status: string): string {
    switch (status) {
      case 'StagingPending':
      case 'StagingInProgress':
        return 'PENDING';
      case 'StagedInEmsx':
        return 'STAGED';
      case 'StagingFailed':
        return 'FAILED';
      default:
        return status.toUpperCase();
    }
  }

  // "StagedInEmsx" -> "STAGED IN EMSX", for the full detail view.
  humanizeStatus(status: string): string {
    return status.replace(/([a-z])([A-Z])/g, '$1 $2').toUpperCase();
  }

  formatTime(isoUtc: string): string {
    const withOffset = isoUtc.endsWith('Z') ? isoUtc : `${isoUtc}Z`;
    return new Date(withOffset).toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit', second: '2-digit' });
  }
}
