import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { OrderIntakeApiService } from '../order-intake-api.service';
import { EmsxMockScenario } from '../models/emsx-mock-scenario.model';

// ADMIN — testing/ops tooling for the mock EMSX gateway only. None of this
// exists once a real (Bloomberg) gateway replaces MockEmsxStagingGateway. No
// real access control yet — this banner is the only thing marking it as
// admin-only today; a later step wires up real identity/authorization.
@Component({
  selector: 'doi-emsx-mock-scenario-admin',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './emsx-mock-scenario-admin.component.html',
  styleUrl: './emsx-mock-scenario-admin.component.scss',
})
export class EmsxMockScenarioAdminComponent implements OnInit {
  private readonly api = inject(OrderIntakeApiService);

  readonly scenarios: { value: EmsxMockScenario; label: string }[] = [
    { value: 'Success', label: 'Success' },
    { value: 'DelayedSuccess', label: 'Delayed success' },
    { value: 'BloombergRejection', label: 'Bloomberg rejection' },
    { value: 'TemporaryConnectionFailure', label: 'Temporary connection failure' },
    { value: 'TimeoutUnknownState', label: 'Timeout / unknown state' },
    { value: 'InvalidInstrument', label: 'Invalid instrument' },
    { value: 'InvalidAccount', label: 'Invalid account' },
    { value: 'DuplicateRequest', label: 'Duplicate request' },
  ];

  readonly scenario = signal<EmsxMockScenario>('Success');
  readonly artificialDelayMs = signal(0);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly savedMessage = signal<string | null>(null);
  readonly errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.api.getEmsxMockScenario().subscribe({
      next: (settings) => {
        this.scenario.set(settings.scenario);
        this.artificialDelayMs.set(settings.artificialDelayMs);
        this.loading.set(false);
      },
      error: (err: unknown) => {
        this.errorMessage.set(err instanceof Error ? err.message : 'Could not load current scenario.');
        this.loading.set(false);
      },
    });
  }

  apply(): void {
    this.saving.set(true);
    this.savedMessage.set(null);
    this.errorMessage.set(null);

    this.api.setEmsxMockScenario({ scenario: this.scenario(), artificialDelayMs: this.artificialDelayMs() }).subscribe({
      next: (settings) => {
        this.scenario.set(settings.scenario);
        this.artificialDelayMs.set(settings.artificialDelayMs);
        this.saving.set(false);
        this.savedMessage.set('Applied — every submit from now on will hit this scenario.');
      },
      error: (err: unknown) => {
        this.saving.set(false);
        this.errorMessage.set(err instanceof Error ? err.message : 'Could not apply scenario.');
      },
    });
  }
}
