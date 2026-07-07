#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Reqnroll.IdeSupport.VisualStudio.NavigationBar;

/// <summary>
/// Static bridge that the Extension project populates so the VSSDK drop-down bar client
/// (which has no reference to <c>LspInterceptingPipe</c>) can fetch document symbols.
/// </summary>
/// <remarks>
/// Set by <c>ReqnrollLanguageClient</c> once the server connection is established;
/// cleared on dispose. Mirrors <see cref="CommentToggleRedirect"/>. Safe to call from any thread.
/// </remarks>
public static class NavigationBarRedirect
{
    /// <summary>
    /// Delegate set by the Extension project: <c>(fileUri, ct) → DocumentSymbol tree</c>.
    /// Null when the server has not yet initialized or has been disposed.
    /// </summary>
    public static Func<string, CancellationToken, Task<IReadOnlyList<GherkinSymbolNode>>>? FetchDocumentSymbolsAsync { get; set; }
}
