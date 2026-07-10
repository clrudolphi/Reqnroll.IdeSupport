#nullable enable

using System;
using OmniSharp.Extensions.LanguageServer.Protocol;

namespace Reqnroll.IdeSupport.LSP.Server.Performance;

/// <summary>
/// No-op <see cref="IOperationDurationRecorder"/>. Used as the default when a handler is
/// constructed without a recorder (unit tests, or a host that does not wire instrumentation),
/// keeping field instrumentation non-intrusive to feature logic.
/// </summary>
public sealed class NullOperationDurationRecorder : IOperationDurationRecorder
{
    public static readonly NullOperationDurationRecorder Instance = new();

    private NullOperationDurationRecorder() { }

    public IDisposable Measure(string operation, DocumentUri? uri = null) => NullScope.Instance;

    public void Record(string operation, double elapsedMs, DocumentUri? uri = null) { }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}
