# 7. Understanding check

1. **What does CQRS separate here, and does it need two databases?**
   Commands (writes through the `Product` entity) from queries (read projections). No; one database and one `CatalogDbContext`.
2. **Why does `GetProductsHandler` use `AsNoTracking` and `Select`?**
   Reads are never saved, so tracking is wasted work; projecting returns only the needed columns as DTOs.
3. **Why order by `Name` and then `Id`?**
   `Name` isn't unique; the unique `Id` tie-breaker makes pages deterministic.
4. **Where is the "stock cannot go negative" rule enforced?**
   In `Product.AdjustStock` (domain), backed by a SQL CHECK constraint; violation returns 400.
5. **What happens if two requests adjust the same product's stock simultaneously?**
   A rowversion token detects the conflict; the loser gets 409 (or 400 if it reads after the winner committed and the rule fails) rather than overwriting. See doc 9.
