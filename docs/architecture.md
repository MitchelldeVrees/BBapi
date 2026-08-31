# Architecture

## Backend layering

```
Api
 ↓
Application
 ↓
Domain
```

- **Domain** (`Dma.OrderIntake.Domain`) — the rich `Order` entity (state only
  changes through `Create()`/`ConfirmInstrument()`/`Submit()`/the staging
  transition methods, so every `Order` in memory already satisfies its
  invariants), `OutboxMessage`, `OrderAuditEvent`, and value objects `Isin`/
  `Mic` (ISO 6166 check digit, ISO 10383 MIC format). No HTTP, no Angular, no
  Bloomberg SDK types, no IdentityServer, no EF Core — just POCOs and pure
  validation logic.
- **Application** (`Dma.OrderIntake.Application`) — use cases: `CreateOrder`,
  `GetOrders`, `GetOrderById`, `GetOrderAuditTrail`, `GetAccounts`,
  `ResolveInstrument`, `ConfirmInstrument`, `SubmitOrder`,
  `Get/SetEmsxMockScenario`. `Validating`, `Accepted`, `ManualReviewRequired`
  etc. from the full spec state machine still don't exist. Application defines
  every port it needs as an interface (`IOrderRepository`,
  `IOutboxRepository`, `IOrderAuditTrail`, `IDmaConnectClient`,
  `IInstrumentResolver`, `IEmsxStagingGateway`, `IEmsxMockScenarioStore`) — it
  never references Infrastructure.
- **Contracts** (`Dma.OrderIntake.Contracts`) — wire shapes shared between Api
  and Angular (`OrderDto`, `CustomerAccount`, `InstrumentMatch`,
  `AuditEventDto`, `EmsxMockScenarioSettings`, ...), mirrored by hand in the
  Angular library's TypeScript models. No project references of its own.
  `StageOrderCommand`/`EmsxStageResult` (the `IEmsxStagingGateway` port's own
  types) live in `Application/Abstractions` instead, since they never cross
  HTTP — an in-process contract can afford to use Domain enums directly.
- **Infrastructure** (`Dma.OrderIntake.Infrastructure`) — implements every
  port, and is the only project whose DI registration is config-driven (see
  "Config-driven adapters" below):
  - `EfOrderRepository` / `EfOutboxRepository` / `EfOrderAuditTrail` — EF Core
    + SQLite. The only project that knows SQLite exists.
  - `MockDmaConnectClient` — 2 demo customers, 5 accounts, fixed GUIDs.
  - `MockInstrumentResolver` / `OpenFigiInstrumentResolver` — both implement
    `IInstrumentResolver`; both reuse `Domain.Isin`/`Mic` for validation so the
    resolve flow itself doesn't change based on which one is active.
  - `MockEmsxStagingGateway` (**SIMULATION — NO REAL ORDERS**, admin-scenario
    driven) / `BloombergEmsxStagingGateway` (a compilable skeleton — see
    "Preparing the real adapters" below) — both implement
    `IEmsxStagingGateway`.
  - `OutboxProcessorWorker` — a `BackgroundService` that polls the outbox (see
    "Reliable, asynchronous order submission").
  - Swapping any mock for the real integration means changing this project
    only — nothing in Application, Api, or Angular needs to know.
- **Api** (`Dma.OrderIntake.Api`) — the composition root. Wires Application
  and Infrastructure together via DI and exposes minimal API endpoints under
  `/api/order-intake/`. A small middleware turns any `DomainException` into a
  `400` with `{ "error": "..." }` — the backend is always the final authority
  on the domain rules (quantity > 0, Market ⇒ no limit price, Limit ⇒ limit
  price required, no submit without a confirmed instrument, ...), never just
  the Angular form.

## Reliable, asynchronous order submission

The danger this avoids: if `POST /submit` called EMSX inline and the response
never came back (dropped connection), the API can't tell "EMSX never got it"
apart from "EMSX got it and staged it" — a naive retry risks a second order.

```
Customer submits order (Idempotency-Key header)
       ↓
DATABASE TRANSACTION
  Order.Submit(key) + Order.MarkStagingPending()
       +
  new OutboxMessage
       ↓
COMMIT  (EfOutboxRepository.EnqueueStagingMessageAsync — one SaveChangesAsync)
       ↓
HTTP 202 Accepted
       ↓
OutboxProcessorWorker (polls every 2s, out of band)
       ↓
IEmsxStagingGateway.StageOrderAsync (mocked, or Bloomberg once real)
```

`SubmitOrderHandler` never calls `IEmsxStagingGateway` — it only ever commits
the order's status change and the outbox message together, atomically, and
returns. The actual EMSX call happens later, entirely out of band, in
`OutboxProcessorWorker`. A failure there marks the order `StagingFailed` (with
a reason) and marks the outbox message processed — terminal for now, not
auto-retried (a real retry policy is future work) — but it can never turn into
a second `Submit()`.

**Idempotency**: every submit carries an `Idempotency-Key` header.
`Order.IdempotencyKey` is stored the first time `Submit()` succeeds (with a
unique DB index as a defense-in-depth backstop). If the exact same key comes
in again — Angular retrying, a flaky network, a double click — `SubmitOrderHandler`
short-circuits and returns the order's current state instead of calling
`Submit()` again. A *different* key against an already-submitted order still
correctly fails (`DomainException`, `400`) — only an exact retry is safe by
design; this doesn't cover truly concurrent identical requests racing each
other (would need a claimed-first-wins ledger row), only the sequential
retry case the spec describes.

Status flow: `Draft → Submitted → StagingPending → StagingInProgress →
StagedInEmsx` (or `StagingInProgress → StagingFailed`).

## Instrument resolution: Resolve ≠ Confirm

`IInstrumentResolver` never returns a single "best guess" — it returns a
status (`Resolved` / `MultipleMatches` / `NotFound` / `Invalid`) plus every
matching candidate. The frontend always requires an explicit "Confirm
instrument" click per candidate, even when there's exactly one match; nothing
auto-selects. `Order.ConfirmInstrument` snapshots the FIGI and instrument name
onto the order at that point (so nothing downstream needs to look anything up
again later), and `Order.CustomerName`/`AccountCode` are snapshotted the same
way at creation time from data the frontend already has.

## Audit trail

Every state-changing use case (`CreateOrder`, `ConfirmInstrument`,
`SubmitOrder`) and the outbox worker itself record an `OrderAuditEvent` —
order ID, event type, a human description, a timestamp, an actor (a
placeholder "system" today — no real identity yet), a `CorrelationId` (one per
order, generated at `Create()`, threaded through every later event including
the ones the background worker records), and a reference (internal order
number / FIGI / EMSX sequence number, whichever is relevant). `GET
/orders/{id}/audit-trail` returns them in order. One thing this doesn't
produce: a standalone "instrument resolved" entry distinct from "instrument
confirmed" — resolving happens before an order exists (see above), so there's
no order yet to attach that event to; it's folded into "InstrumentConfirmed".

## Config-driven adapters

```json
"OrderIntake": {
  "InstrumentResolver": "Mock",       // "Mock" | "OpenFigi"
  "Emsx": { "Environment": "Mock" }   // "Mock" | "Beta" | "Production"
}
```

`Infrastructure.DependencyInjection.AddInfrastructure` reads this once at
startup and registers the matching implementation — nothing above
Infrastructure changes based on which one is active. Verified live, both ways:

- `OpenFigiInstrumentResolver` is a genuinely working call to the real, public
  OpenFIGI mapping API (not a skeleton — OpenFIGI isn't proprietary). Known
  gaps: OpenFIGI's mapping endpoint doesn't return currency, and its
  `exchCode` is Bloomberg's own exchange code, not an ISO 10383 MIC — both are
  surfaced as `"UNKNOWN"` rather than guessed at.
- `BloombergEmsxStagingGateway` is deliberately just a **compilable adapter
  boundary** — the real Bloomberg EMSX SDK/BLPAPI is proprietary and not
  available here, so no Bloomberg SDK types (`BloombergSession`,
  `BloombergRequest`, `BloombergElement`, ...) appear anywhere in this
  codebase. It throws a clear `NotSupportedException` naming the environment
  and which service it would have used (`//blp/emapisvc_beta` for `Beta`,
  `//blp/emapisvc` for `Production` — both still need `//blp/apiauth`). Scope
  for Goal 1 is strictly `CreateOrder` (staging the parent order only) —
  explicitly not `CreateOrderAndRouteEx` / `RouteEx`.

## Frontend layering

- **`projects/dma-order-intake`** — the Angular library; all real
  functionality lives here. `OrderIntakeApiService` wraps every endpoint.
  `AccountSelectorComponent` and `InstrumentResolverComponent` are reusable
  building blocks — the latter has two modes: given an `orderId` it confirms
  against a live order immediately, without one (used inside the wizard) it
  just emits the customer's explicit choice locally. `NewOrderWizardComponent`
  composes both plus its own Order-details/Review/Submit steps into the full
  five-screen flow (Account → Instrument → Order details → Review → Submit),
  turning local wizard state into `CreateOrder` → `ConfirmInstrument` →
  `SubmitOrder` as one sequence only once the customer clicks "Submit order".
  A failed submit offers "Retry submit", reusing the same order id and
  idempotency key. `OrderOverviewComponent` is the order-management screen:
  a filterable list (status/date/account/instrument/customer reference) that
  drills into a detail view with the full audit trail.
  `EmsxMockScenarioAdminComponent` is the admin-only panel driving
  `MockEmsxStagingGateway`'s scenario — no real access control yet, just a
  clearly labeled admin route/banner.
- **`apps/dma-order-intake-demo`** — a thin shell hosting all three
  (`<doi-new-order-wizard>`, `<doi-order-overview>`, `<doi-emsx-mock-scenario-admin>`)
  behind simple tabs. No business logic of its own.

## Proving it end to end

`docker compose up --build` starts two containers (`api` on `localhost:5158`,
`frontend` on `localhost:4200`, SQLite persisted on the `api-data` volume).
Opening `http://localhost:4200`, setting a mock EMSX scenario, and running the
wizard end to end — pick an account, resolve+confirm ASML (`NL0010273215` /
`XAMS`), fill in order details, review, submit — creates a real, structured
`Order` in SQLite, watches its status progress live (including a visible
`StagingInProgress` window when an artificial delay is configured), and shows
up in the order overview with a full, correctly-ordered audit trail. A
"SIMULATION — NO REAL ORDERS" banner is visible throughout the wizard.
