import { HttpClient } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ORDER_INTAKE_API_BASE_URL } from './order-intake-api.config';
import { AuditEventDto } from './models/audit-event.model';
import { CustomerAccount } from './models/customer-account.model';
import { EmsxMockScenarioSettings } from './models/emsx-mock-scenario.model';
import { ConfirmInstrumentRequest, InstrumentResolutionRequest, InstrumentResolutionResult } from './models/instrument.model';
import { CreateOrderRequest, OrderDto } from './models/order.model';

// This is the real functionality the library exposes. The demo app is only a
// shell that calls into this service to prove the wiring works end to end.
@Injectable({ providedIn: 'root' })
export class OrderIntakeApiService {
  private readonly ordersUrl: string;
  private readonly accountsUrl: string;
  private readonly resolveInstrumentUrl: string;
  private readonly adminEmsxMockScenarioUrl: string;

  constructor(
    private readonly http: HttpClient,
    @Inject(ORDER_INTAKE_API_BASE_URL) private readonly apiBaseUrl: string,
  ) {
    this.ordersUrl = `${this.apiBaseUrl}/api/order-intake/orders`;
    this.accountsUrl = `${this.apiBaseUrl}/api/order-intake/accounts`;
    this.resolveInstrumentUrl = `${this.apiBaseUrl}/api/order-intake/instruments/resolve`;
    this.adminEmsxMockScenarioUrl = `${this.apiBaseUrl}/api/order-intake/admin/emsx-mock-scenario`;
  }

  getOrders(): Observable<OrderDto[]> {
    return this.http.get<OrderDto[]>(this.ordersUrl);
  }

  getOrderById(id: string): Observable<OrderDto> {
    return this.http.get<OrderDto>(`${this.ordersUrl}/${id}`);
  }

  createOrder(request: CreateOrderRequest): Observable<OrderDto> {
    return this.http.post<OrderDto>(this.ordersUrl, request);
  }

  // Returns 202 with the order already in StagingPending — EMSX staging
  // itself happens later, out of band. See docs/architecture.md.
  //
  // idempotencyKey: pass the SAME value on a retry of the SAME logical submit
  // (e.g. after this call itself failed) — the backend then returns the
  // order's current state instead of attempting Submit() a second time.
  submitOrder(orderId: string, idempotencyKey: string): Observable<OrderDto> {
    return this.http.post<OrderDto>(
      `${this.ordersUrl}/${orderId}/submit`,
      {},
      { headers: { 'Idempotency-Key': idempotencyKey } },
    );
  }

  getOrderAuditTrail(orderId: string): Observable<AuditEventDto[]> {
    return this.http.get<AuditEventDto[]>(`${this.ordersUrl}/${orderId}/audit-trail`);
  }

  // Backed by MockDmaConnectClient today; same shape once the real dmaConnect
  // integration lands, so this call site doesn't change.
  getAccounts(): Observable<CustomerAccount[]> {
    return this.http.get<CustomerAccount[]>(this.accountsUrl);
  }

  // Backed by MockInstrumentResolver (or OpenFigiInstrumentResolver, per
  // backend config) today. Never attaches anything to an order by itself —
  // see confirmInstrument.
  resolveInstrument(request: InstrumentResolutionRequest): Observable<InstrumentResolutionResult> {
    return this.http.post<InstrumentResolutionResult>(this.resolveInstrumentUrl, request);
  }

  confirmInstrument(orderId: string, request: ConfirmInstrumentRequest): Observable<OrderDto> {
    return this.http.post<OrderDto>(`${this.ordersUrl}/${orderId}/confirm-instrument`, request);
  }

  // ADMIN — testing/ops tooling for the mock EMSX gateway only.
  getEmsxMockScenario(): Observable<EmsxMockScenarioSettings> {
    return this.http.get<EmsxMockScenarioSettings>(this.adminEmsxMockScenarioUrl);
  }

  setEmsxMockScenario(settings: EmsxMockScenarioSettings): Observable<EmsxMockScenarioSettings> {
    return this.http.post<EmsxMockScenarioSettings>(this.adminEmsxMockScenarioUrl, settings);
  }
}
