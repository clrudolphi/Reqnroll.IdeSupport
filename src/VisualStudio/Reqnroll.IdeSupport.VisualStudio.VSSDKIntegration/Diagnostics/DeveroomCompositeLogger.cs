using System.Diagnostics;
using Reqnroll.IdeSupport.Common.Diagnostics;
using System.Collections;
using System.ComponentModel.Composition;

namespace Reqnroll.IdeSupport.VisualStudio.Diagnostics;

/// <summary>
/// The single, MEF-composable <see cref="IDeveroomLogger"/> sink for the legacy VSSDK/MEF
/// composition root (issue #84): previously exported but never populated with child loggers, so
/// every MEF import of this type was silently a no-op. Now wires the same
/// debug-output + synchronous-file-logger pair (at the same default level) used by the
/// Extensibility-SDK side (see <c>ExtensionEntrypoint.InitializeServices</c>), so both composition
/// roots share one consistent default instead of each having their own ad-hoc loggers.
/// </summary>
[Export(typeof(IDeveroomLogger))]
[Export(typeof(DeveroomCompositeLogger))]
public class DeveroomCompositeLogger : Reqnroll.IdeSupport.Common.Diagnostics.DeveroomCompositeLogger
{
    public DeveroomCompositeLogger()
    {
        Add(new DeveroomDebugLogger());
        Add(new SynchronousFileLogger("vs", "ext", TraceLevel.Info));
    }
}