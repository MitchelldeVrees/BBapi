// Mirrors Dma.OrderIntake.Contracts.* on the backend.
export type InstrumentResolutionStatus = 'Resolved' | 'MultipleMatches' | 'NotFound' | 'Invalid';

export interface InstrumentMatch {
  instrumentId: string;
  isin: string;
  mic: string;
  name: string;
  currency: string;
  securityType: string;
  ticker: string;
  figi: string;
}

export interface InstrumentResolutionResult {
  status: InstrumentResolutionStatus;
  errorMessage: string | null;
  matches: InstrumentMatch[];
}

export interface InstrumentResolutionRequest {
  isin: string;
  mic?: string | null;
}

export interface ConfirmInstrumentRequest {
  instrumentId: string;
  figi: string;
  instrumentName: string;
}
