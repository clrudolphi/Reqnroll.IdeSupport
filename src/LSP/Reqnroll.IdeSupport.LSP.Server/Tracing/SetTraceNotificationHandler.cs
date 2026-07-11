#nullable enable

using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Reqnroll.IdeSupport.LSP.Server.Tracing;

/// <summary>
/// Handles the standard <c>$/setTrace</c> notification (F41), letting the client change the
/// trace level at runtime without restarting the server.
/// </summary>
public sealed class SetTraceNotificationHandler : SetTraceHandlerBase
{
    private readonly ITraceService _traceService;

    /// <summary>Initializes a new instance of the <see cref="SetTraceNotificationHandler"/> class.</summary>
    public SetTraceNotificationHandler(ITraceService traceService)
    {
        _traceService = traceService;
    }

    /// <summary>Handles a <c>$/setTrace</c> notification by updating the shared <see cref="ITraceService"/>'s trace level at runtime.</summary>
    public override Task<Unit> Handle(SetTraceParams request, CancellationToken cancellationToken)
    {
        _traceService.Level = request.Value;
        return Unit.Task;
    }
}
