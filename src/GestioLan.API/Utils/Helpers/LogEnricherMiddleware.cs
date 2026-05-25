using Serilog.Context;
using GestioLan.API.Utils.Helpers;

namespace GestioLan.API.Utils.Helpers;

public class LogEnricherMiddleware
{
    private readonly RequestDelegate _next;

    public LogEnricherMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ICurrentUserService currentUserService)
    {
        // 1. Prendiamo l'endpoint corrente per capire quale Controller sta gestendo la richiesta
        var endpoint = context.GetEndpoint();

        var descriptor = endpoint?.Metadata
            .GetMetadata<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>();

        string controllerName = descriptor?.ControllerName ?? "System";
        string actionName = descriptor?.ActionName ?? "Unknown";

        // 2. Spingiamo User e Service nello "zainetto" (LogContext)
        using (LogContext.PushProperty("User", currentUserService.Username))
        using (LogContext.PushProperty("Service", $"{controllerName}"))
        using (LogContext.PushProperty("Action", actionName))
        {
            // Tutto ciò che succede da qui in poi (Controller, Service, Database)
            // erediterà queste due proprietà in automatico!
            await _next(context);
        }
    }
}