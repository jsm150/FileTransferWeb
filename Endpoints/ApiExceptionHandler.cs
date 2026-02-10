using FileTransferWeb.Domain.Shared;
using FileTransferWeb.Storage.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace FileTransferWeb.Endpoints;

public sealed class ApiExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService = problemDetailsService;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (!httpContext.Request.Path.StartsWithSegments("/api"))
        {
            return false;
        }

        var (statusCode, title, detail) = MapException(exception);

        httpContext.Response.StatusCode = statusCode;

        return await _problemDetailsService.TryWriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = new ProblemDetails
                {
                    Status = statusCode,
                    Title = title,
                    Detail = detail
                },
                Exception = exception
            });
    }

    private static (int StatusCode, string Title, string Detail) MapException(Exception exception)
    {
        return exception switch
        {
            StorageDomainException storageDomainException =>
                (StatusCodes.Status400BadRequest, "잘못된 디렉터리 경로 요청입니다.", storageDomainException.Message),
            DomainException domainException =>
                (StatusCodes.Status400BadRequest, "도메인 규칙을 위반한 요청입니다.", domainException.Message),
            DirectoryNotFoundException =>
                (StatusCodes.Status404NotFound, "디렉터리를 찾을 수 없습니다.", "요청한 디렉터리가 존재하지 않습니다."),
            _ =>
                (StatusCodes.Status500InternalServerError, "디렉터리 목록 조회에 실패했습니다.", "잠시 후 다시 시도해주세요.")
        };
    }
}
