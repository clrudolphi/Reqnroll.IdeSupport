#nullable enable

using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Reqnroll.IdeSupport.LSP.Server.Tracing;

/// <inheritdoc cref="ITraceService"/>
public sealed class TraceService : ITraceService
{
    private readonly ILanguageServerFacade _languageServer;
    private volatile InitializeTrace _level = InitializeTrace.Off;

    /// <summary>Initializes a new instance of the <see cref="TraceService"/> class.</summary>
    public TraceService(ILanguageServerFacade languageServer, InitializeTrace initialLevel = InitializeTrace.Off)
    {
        _languageServer = languageServer;
        _level = initialLevel;
    }

    /// <summary>Gets or sets the level.</summary>
    public InitializeTrace Level
    {
        get => _level;
        set => _level = value;
    }

    /// <summary>Sends a <c>$/logTrace</c> notification to the client if tracing is enabled, including the verbose message when the trace level is <see cref="InitializeTrace.Verbose"/>.</summary>
    public void Trace(string message, System.Func<string>? verboseMessage = null)
    {
        var level = _level;
        if (level == InitializeTrace.Off)
            return;

        // ILanguageServerFacade doesn't implement ILanguageServer directly (the generated
        // LogTrace(this ILanguageServer, ...) extension can't target it), but it does implement
        // IResponseRouter — the same interface that extension delegates to internally.
        _languageServer.SendNotification(new LogTraceParams
        {
            Message = message,
            Verbose = level == InitializeTrace.Verbose ? verboseMessage?.Invoke() : null
        });
    }
}
