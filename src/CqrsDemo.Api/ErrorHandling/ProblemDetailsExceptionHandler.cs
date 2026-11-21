using CqrsDemo.Application.Common;
using CqrsDemo.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace CqrsDemo.Api.ErrorHandling;

/// <summary>
/// The one place where exceptions thrown by the Domain and Application layers become HTTP responses.
/// Those layers don't know about HTTP; this class is the translator. Every error leaves the API
/// as application/problem+json, so clients handle a single error shape.
/// </summary>
public sealed class ProblemDetailsExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails problem = exception switch
        {
            DomainValidationException ex => Validation(ex.Errors),
            RequestValidationException ex => Validation(ex.Errors),
            NotFoundException ex => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Resource not found.",
                Detail = ex.Message,
            },
            ConcurrencyConflictException ex => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "The resource was changed by another request.",
                Detail = ex.Message,
            },
            BadHttpRequestException => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "The request body could not be read.",
                Detail = "Send valid JSON that matches the documented request shape.",
            },
            _ => null!,
        };

        // Anything unexpected is not handled here: the default handler returns a generic 500.
        if (problem is null)
        {
            return false;
        }

        httpContext.Response.StatusCode = problem.Status!.Value;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    private static ProblemDetails Validation(IReadOnlyDictionary<string, string[]> errors) =>
        new HttpValidationProblemDetails(errors.ToDictionary(e => e.Key, e => e.Value))
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
        };
}
