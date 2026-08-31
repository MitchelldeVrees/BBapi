import { Component, OnInit, signal } from '@angular/core';
import {
  EmsxMockScenarioAdminComponent,
  NewOrderWizardComponent,
  OrderDto,
  OrderIntakeApiService,
  OrderOverviewComponent,
} from 'dma-order-intake';

type ConnectionState = 'checking' | 'connected' | 'error';
type Tab = 'new-order' | 'orders';

// This app is only a shell: it hosts the dma-order-intake library standalone
// and proves Angular -> Dma.OrderIntake.Api -> SQLite works end to end. All
// real functionality — the New Order wizard, the order overview, the admin
// mock-scenario panel — lives in the library, not here.
@Component({
  selector: 'app-root',
  imports: [NewOrderWizardComponent, OrderOverviewComponent, EmsxMockScenarioAdminComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
})
export class AppComponent implements OnInit {
  readonly state = signal<ConnectionState>('checking');
  readonly orders = signal<OrderDto[]>([]);
  readonly errorMessage = signal<string | null>(null);
  readonly tab = signal<Tab>('new-order');

  constructor(private readonly orderIntakeApi: OrderIntakeApiService) {}

  ngOnInit(): void {
    this.orderIntakeApi.getOrders().subscribe({
      next: (orders) => {
        this.orders.set(orders);
        this.state.set('connected');
      },
      error: (err) => {
        this.errorMessage.set(err.message ?? 'Unknown error');
        this.state.set('error');
      },
    });
  }
}
