import { InjectionToken, Provider } from '@angular/core';

// The base URL of the Dma.OrderIntake.Api backend. The consuming app (the demo
// shell, or later a real host app) provides this — the library never hardcodes it.
export const ORDER_INTAKE_API_BASE_URL = new InjectionToken<string>('ORDER_INTAKE_API_BASE_URL');

export function provideOrderIntakeApi(apiBaseUrl: string): Provider {
  return { provide: ORDER_INTAKE_API_BASE_URL, useValue: apiBaseUrl };
}
