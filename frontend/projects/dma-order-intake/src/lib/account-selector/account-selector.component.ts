import { Component, OnInit, inject, output, signal } from '@angular/core';
import { OrderIntakeApiService } from '../order-intake-api.service';
import { CustomerAccount } from '../models/customer-account.model';

interface CustomerGroup {
  customerId: string;
  customerName: string;
  accounts: CustomerAccount[];
}

// Real functionality, not demo-app scaffolding: "which customer/account is
// this order for" has to be answered before an order-entry form makes sense.
// Sourced from IDmaConnectClient (currently MockDmaConnectClient) via the Api.
@Component({
  selector: 'doi-account-selector',
  standalone: true,
  templateUrl: './account-selector.component.html',
  styleUrl: './account-selector.component.scss',
})
export class AccountSelectorComponent implements OnInit {
  private readonly api = inject(OrderIntakeApiService);

  readonly accountSelected = output<CustomerAccount>();

  readonly customerGroups = signal<CustomerGroup[]>([]);
  readonly selectedAccountId = signal<string | null>(null);
  readonly loading = signal(true);
  readonly errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.api.getAccounts().subscribe({
      next: (accounts) => {
        this.customerGroups.set(this.groupByCustomer(accounts));
        this.loading.set(false);
      },
      error: (err: unknown) => {
        this.errorMessage.set(err instanceof Error ? err.message : 'Could not load accounts.');
        this.loading.set(false);
      },
    });
  }

  select(account: CustomerAccount): void {
    this.selectedAccountId.set(account.accountId);
    this.accountSelected.emit(account);
  }

  private groupByCustomer(accounts: CustomerAccount[]): CustomerGroup[] {
    const groups = new Map<string, CustomerGroup>();

    for (const account of accounts) {
      let group = groups.get(account.customerId);
      if (!group) {
        group = { customerId: account.customerId, customerName: account.customerName, accounts: [] };
        groups.set(account.customerId, group);
      }
      group.accounts.push(account);
    }

    return [...groups.values()];
  }
}
