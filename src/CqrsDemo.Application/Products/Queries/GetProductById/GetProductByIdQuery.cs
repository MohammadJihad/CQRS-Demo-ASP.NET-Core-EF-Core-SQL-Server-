namespace CqrsDemo.Application.Products.Queries.GetProductById;

/// <summary>Question: "what does product X look like right now?"</summary>
public sealed record GetProductByIdQuery(Guid Id);
