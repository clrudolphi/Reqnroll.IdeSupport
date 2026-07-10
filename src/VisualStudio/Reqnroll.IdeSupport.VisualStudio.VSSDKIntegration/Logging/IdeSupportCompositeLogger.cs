using System.ComponentModel.Composition;
using Reqnroll.IdeSupport.Common.Logging;

namespace Reqnroll.IdeSupport.VisualStudio.Logging;

/// <summary>
/// The single, MEF-composable <see cref="IIdeSupportLogger"/> sink for the legacy VSSDK/MEF
/// composition root (issue #84): previously exported but never populated with child loggers, so
/// every MEF import of this type was silently a no-op. Now wires the same
/// debug-output + synchronous-file-logger pair (at the same default level) used by the
/// Extensibility-SDK side (see <c>ExtensionEntrypoint.InitializeServices</c>), so both composition
/// roots share one consistent default instead of each having their own ad-hoc loggers.
/// </summary>
[Export(typeof(IIdeSupportLogger))]
[Export(typeof(IdeSupportCompositeLogger))]
public class IdeSupportCompositeLogger : Reqnroll.IdeSupport.Common.Logging.IdeSupportCompositeLogger
{
    /// <summary>
    /// Wires up the default debug-output and synchronous-file-logger sinks.
    /// </summary>
    public IdeSupportCompositeLogger()
    {
        Add(new IdeSupportDebugLogger());
        Add(new SynchronousFileLogger("vs", "ext", TraceLevel.Info));
    }
}