// Mirrors Dma.OrderIntake.Contracts.EmsxMockScenario(Settings) on the backend.
// Admin-only test-scenario switch for the mock EMSX gateway — none of this
// exists once a real integration replaces it.
export type EmsxMockScenario =
  | 'Success'
  | 'DelayedSuccess'
  | 'BloombergRejection'
  | 'TemporaryConnectionFailure'
  | 'TimeoutUnknownState'
  | 'InvalidInstrument'
  | 'InvalidAccount'
  | 'DuplicateRequest';

export interface EmsxMockScenarioSettings {
  scenario: EmsxMockScenario;
  artificialDelayMs: number;
}
