# Payment Gateway: Technical Overview

## Architecture

The solution follows **Clean / Onion Architecture**. Each layer is a separate project, so the dependency rules are enforced by the compiler through project references rather than by convention.

**The dependency rule:** source code dependencies only point **inwards**. Inner layers know nothing about outer layers. Domain has no idea there is an HTTP API or an in-memory store.

### Layers

```
┌───────────────────────────────────────────────────────────┐
│  Presentation (PaymentGateway.Api)                        │
│  Controllers · DTOs · Mappings · Program.cs               │
│   ┌───────────────────────────────────────────────────┐   │
│   │  Infrastructure (PaymentGateway.Infrastructure)   │   │
│   │  PaymentsRepository · (later) BankClient          │   │
│   │   ┌───────────────────────────────────────────┐   │   │
│   │   │  Application (PaymentGateway.Application) │   │   │
│   │   │  PaymentService · IPaymentsRepository     │   │   │
│   │   │   ┌───────────────────────────────────┐   │   │   │
│   │   │   │  Domain (PaymentGateway.Domain)   │   │   │   │
│   │   │   │  Payment · PaymentStatus          │   │   │   │
│   │   │   └───────────────────────────────────┘   │   │   │
│   │   └───────────────────────────────────────────┘   │   │
│   └───────────────────────────────────────────────────┘   │
└───────────────────────────────────────────────────────────┘
        Common (PaymentGateway.Common): shared, no dependencies
```

### Project dependencies

Solid arrows are project references ("depends on"). The dashed arrow is an interface implementation: Infrastructure implements an interface that Application owns. This is **dependency inversion**, and it is how Application can use storage without referencing Infrastructure.

```mermaid
flowchart TD
    Api["<b>Presentation</b><br/>PaymentGateway.Api<br/><i>Controllers, DTOs, Mappings, composition root</i>"]
    Infra["<b>Infrastructure</b><br/>PaymentGateway.Infrastructure<br/><i>PaymentsRepository</i>"]
    App["<b>Application</b><br/>PaymentGateway.Application<br/><i>PaymentService, IPaymentsRepository</i>"]
    Domain["<b>Domain</b><br/>PaymentGateway.Domain<br/><i>Payment, PaymentStatus</i>"]
    Common["<b>Common</b><br/>PaymentGateway.Common<br/><i>shared building blocks</i>"]

    Api --> App
    Api -- "DI registration only" --> Infra
    Infra --> App
    App --> Domain
    App --> Common
    Domain --> Common
    Infra -. "implements IPaymentsRepository" .-> App
```

| Project | Responsibility | References |
|---|---|---|
| `PaymentGateway.Domain` | Business entities and rules (`Payment`, `PaymentStatus`). No framework dependencies. | Common |
| `PaymentGateway.Application` | Use cases (`PaymentService`) and the interfaces it needs from the outside world (`IPaymentsRepository`). | Domain, Common |
| `PaymentGateway.Infrastructure` | Implementations of Application's interfaces: storage now, the bank HTTP client later. | Application |
| `PaymentGateway.Api` | HTTP concerns: routing, request and response contracts, status codes, and explicit mapping from domain to contract (`Mappings/`). Also the **composition root** that wires everything together in `Program.cs`. | Application, Infrastructure |
| `PaymentGateway.Common` | Cross-cutting building blocks shared by inner layers. | — |

> **Why does Api reference Infrastructure?** Only so `Program.cs` can call `AddInfrastructure()` to register implementations. Controllers depend on `IPaymentService` and never use Infrastructure types directly.

### Runtime flow: `GET /api/payments/{id}`

At runtime, calls flow outside → in → out again. At compile time, every arrow still points inwards.

```mermaid
sequenceDiagram
    participant Client as Merchant
    participant C as PaymentsController<br/>(Api)
    participant S as PaymentService<br/>(Application)
    participant R as IPaymentsRepository<br/>(Application interface)
    participant Impl as PaymentsRepository<br/>(Infrastructure)

    Client->>C: GET /api/payments/{id}
    C->>S: GetPayment(id)
    S->>R: Get(id)
    R->>Impl: resolved by DI
    Impl-->>S: Payment?
    S-->>C: Payment?
    alt found
        C->>C: payment.ToGetPaymentResponse()
        C-->>Client: 200 OK + GetPaymentResponse
    else not found
        C-->>Client: 404 ProblemDetails
    end
```

### What this buys us

- **Testability:** `PaymentService` can be unit tested with a fake `IPaymentsRepository` (and later a fake bank client), with no HTTP or storage involved.
- **Replaceable infrastructure:** swapping the in-memory store for a database, or the bank simulator for a real acquirer, only touches Infrastructure and its DI registration.
- **Stable API contracts:** request and response DTOs live in the Api project, so the HTTP contract can change without touching the domain, and the reverse. Controllers never serialise domain entities: they map explicitly (`payment.ToGetPaymentResponse()`), so internal fields can't leak into responses by accident. Mapping is hand-written rather than AutoMapper, so it is compile-checked and easy to read.
