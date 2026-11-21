# 4. CQRS vs CRUD, Mediator and event sourcing

| Topic | CRUD | This CQRS sample |
|---|---|---|
| Model | One entity for everything | Entity for writes, DTO projections for reads |
| Rules | Often in controllers | In `Product` and handlers |
| Reads | Load entities | `AsNoTracking` + `Select` |
| Cost | Simplest | More types and files |

**Benefits:** focused handlers, read shapes tuned independently, easy to test, a path to scale reads later.
**Limitations:** more files for simple apps; the benefit is small until read/write needs diverge.

**CQRS vs Mediator.** CQRS is a *design principle*; a mediator (e.g. MediatR) is a *library* that dispatches messages to handlers. This sample uses plain classes injected directly, so you can follow the call stack with F12.

**CQRS vs event sourcing.** Event sourcing stores state as a sequence of events instead of current rows. It is often combined with CQRS but is independent; this sample stores ordinary rows in SQL Server.
