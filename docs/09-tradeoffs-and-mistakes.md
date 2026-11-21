# 9. Trade-offs, concurrency and common mistakes

## Concurrent stock updates
`Product` has a SQL `rowversion` column used as an optimistic concurrency token (`ProductConfiguration.cs`). If another writer changed the row between read and save, EF raises `DbUpdateConcurrencyException`; `AdjustProductStockHandler` converts it to `ConcurrencyConflictException`, and `ProblemDetailsExceptionHandler` returns **409 Conflict**.

Trade-off: no locks are held, so throughput is good and no oversell/lost update occurs, but clients must retry on 409. Alternatives (not implemented): an atomic `UPDATE ... SET Stock = Stock + @n WHERE Stock + @n >= 0` via `ExecuteUpdateAsync`, pessimistic locking, or automatic retry.

Verified by integration tests (`ProductApiTests`): a concurrent-stock race (stock never goes negative) and a stale-write rowversion test.

## Known limitations
- No authentication; sample only.
- Page size is capped; no cursor paging.
- Name filter is a contains search (not index-seekable).

## Common mistakes
- Returning tracked entities from queries.
- Putting rules only in controllers or only in the database.
- Querying inside a command handler to shape a response DTO.
- Ignoring `CancellationToken`.
- Using `float` for money (this sample uses `decimal(18,2)`).
- Running `dotnet ef ... --no-build` against a stale build.
