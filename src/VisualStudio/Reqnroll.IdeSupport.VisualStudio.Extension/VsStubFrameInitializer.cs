#nullable enable

using System.Diagnostics;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Reqnroll.IdeSupport.VisualStudio.Extension;

/// <summary>
/// Scans the VS Running Document Table (RDT) for <c>.feature</c> stub frames — documents
/// that were restored from a previous session but whose text buffer has not yet been
/// initialized — and force-initializes them so the LSP's <c>textDocument/didOpen</c> fires.
/// </summary>
/// <remarks>
/// Shared by <see cref="ReqnrollPluginPackage"/> (Piece 1, early activation)
/// and <see cref="ReqnrollLanguageClient.OnServerInitializationResultAsync"/> (Piece 2,
/// post-server-init stub flush). All public methods must be called from the UI thread.
/// </remarks>
internal static class VsStubFrameInitializer
{
    /// <summary>
    /// Forces initialization of <c>.feature</c> stub frames in the RDT, or falls back to
    /// opening a <c>.feature</c> file from the project hierarchy if no stubs exist.
    /// </summary>
    public static async Task EnsureFeatureFileActivatedAsync(
        IServiceProvider serviceProvider,
        TraceSource traceSource,
        CancellationToken cancellationToken)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        // Stage 1: scan the RDT for .feature stub frames
        var rdt = serviceProvider.GetService(typeof(SVsRunningDocumentTable)) as IVsRunningDocumentTable;
        if (rdt != null && TryForceInitRdtStubs(rdt, serviceProvider, traceSource))
            return;

        // Stage 2: fallback — open the first .feature file from project items
        await TryOpenFirstFeatureFileAsync(serviceProvider, traceSource, cancellationToken);
    }

    /// <summary>
    /// Forces initialization of <c>.feature</c> stub frames discovered via RDT scan.
    /// Called after the LSP server initialises to flush remaining background stubs.
    /// </summary>
    public static async Task ForceInitFeatureStubsAsync(
        IServiceProvider serviceProvider,
        TraceSource traceSource,
        CancellationToken cancellationToken)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var rdt = serviceProvider.GetService(typeof(SVsRunningDocumentTable)) as IVsRunningDocumentTable;
        if (rdt != null)
            TryForceInitRdtStubs(rdt, serviceProvider, traceSource);
    }

    // ── Stage 1: RDT stub scan ─────────────────────────────────────────────

    private static bool TryForceInitRdtStubs(
        IVsRunningDocumentTable rdt,
        IServiceProvider serviceProvider,
        TraceSource traceSource)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        rdt.GetRunningDocumentsEnum(out var enumDocs);
        if (enumDocs is null)
            return false;

        var cookies = new uint[1];
        var anyFound = false;

        while (enumDocs.Next(1, cookies, out var fetched) == VSConstants.S_OK && fetched == 1)
        {
            var cookie = cookies[0];

            rdt.GetDocumentInfo(cookie, out _, out _, out _, out var moniker, out _, out _, out var docData);

            if (moniker is null || !moniker.EndsWith(".feature", StringComparison.OrdinalIgnoreCase))
                continue;

            anyFound = true;

            // If document data is already initialized, skip.
            if (docData != IntPtr.Zero)
            {
                traceSource.TraceInformation(
                    "VsStubFrameInitializer: '{0}' is already initialized — skipping.", moniker);
                continue;
            }

            // Force-initialize: use IsDocumentOpen to get the window frame, then
            // request its DocData property. This triggers VS to fully initialize the
            // document (text buffer, content type, etc.) which in turn fires
            // textDocument/didOpen to the LSP client.
            if (VsShellUtilities.IsDocumentOpen(serviceProvider, moniker, Guid.Empty,
                    out var hier, out _, out var frame))
            {
                traceSource.TraceInformation(
                    "VsStubFrameInitializer: forcing init of stub '{0}' via window frame.", moniker);
                _ = frame.GetProperty((int)__VSFPROPID.VSFPROPID_DocData, out var _);

                // After initialization, ensure the document has a real project hierarchy
                // (not the miscellaneous-files bucket).  If it doesn't, reopen via DTE
                // which registers the document with the owning project's IVsHierarchy.
                if (hier == null || IsMiscellaneousFilesProject(hier))
                {
                    try
                    {
                        var dte = serviceProvider.GetService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
                        if (dte != null)
                        {
                            traceSource.TraceInformation(
                                "VsStubFrameInitializer: reopening '{0}' through DTE for project context.", moniker);
                            dte.ItemOperations.OpenFile(moniker);
                        }
                    }
                    catch (Exception ex)
                    {
                        traceSource.TraceEvent(TraceEventType.Warning, 0,
                            "VsStubFrameInitializer: could not set project context for '{0}': {1}", moniker, ex.Message);
                    }
                }
            }
        }

        return anyFound;
    }

    // ── Stage 2: project-item fallback ──────────────────────────────────────

#pragma warning disable VSTHRD010 // Called from the UI thread only (entry points switch first).
#pragma warning disable VSSDK006 // GetService result null-checked by 'as DTE2' below.
    private static async Task TryOpenFirstFeatureFileAsync(
        IServiceProvider serviceProvider,
        TraceSource traceSource,
        CancellationToken cancellationToken)
    {
        var dte2 = serviceProvider.GetService(typeof(DTE)) as DTE2;
        if (dte2?.Solution is null)
            return;

        // Find the first .feature file in the solution by enumerating project items.
        var candidates = new List<string>();
        EnumerateProjectItems(dte2.Solution.Projects, ".feature", candidates, traceSource);

        if (candidates.Count == 0)
        {
            traceSource.TraceInformation(
                "VsStubFrameInitializer: no .feature files found in solution projects.");
            return;
        }

        // Open the first .feature file to trigger CreateServerConnectionAsync.
        var first = candidates[0];
        traceSource.TraceInformation(
            "VsStubFrameInitializer: opening '{0}' to trigger LSP activation.", first);

        try
        {
            var doc = dte2.Documents.Open(first);
            if (doc is not null)
            {
                // Give the LSP a moment to pick up the didOpen, then close.
                await Task.Delay(500, cancellationToken).ConfigureAwait(false);
                doc.Close(vsSaveChanges.vsSaveChangesNo);
            }
        }
        catch (Exception ex)
        {
            traceSource.TraceEvent(TraceEventType.Warning, 0,
                "VsStubFrameInitializer: fallback open failed: {0}", ex.Message);
        }
    }

    // These methods access DTE COM objects that require the UI thread.
    // They are only called from TryOpenFirstFeatureFileAsync which runs after
    // SwitchToMainThreadAsync.
#pragma warning disable VSTHRD010
    private static void EnumerateProjectItems(
        Projects? projects,
        string extension,
        List<string> results,
        TraceSource traceSource)
    {
        if (projects is null)
            return;

        foreach (Project project in projects)
        {
            if (project is null)
                continue;

            try
            {
                EnumerateProjectItems(project.ProjectItems, extension, results, traceSource);
            }
            catch (Exception ex)
            {
                traceSource.TraceEvent(TraceEventType.Warning, 0,
                    "VsStubFrameInitializer: skipping project '{0}': {1}", GetProjectName(project), ex.Message);
            }
        }
    }

    private static void EnumerateProjectItems(
        ProjectItems? items,
        string extension,
        List<string> results,
        TraceSource traceSource)
    {
        if (items is null)
            return;

        foreach (ProjectItem item in items)
        {
            if (item is null)
                continue;

            try
            {
                var name = item.Name;
                if (name is not null && name.EndsWith(extension, StringComparison.OrdinalIgnoreCase) &&
                    item.FileCount > 0)
                {
                    results.Add(item.FileNames[0]);
                }

                // Recurse into sub-items (sub-folders, nested files).
                if (item.ProjectItems is { Count: > 0 })
                    EnumerateProjectItems(item.ProjectItems, extension, results, traceSource);
            }
            catch (Exception ex)
            {
                traceSource.TraceEvent(TraceEventType.Warning, 0,
                    "VsStubFrameInitializer: skipping item '{0}': {1}", GetItemName(item), ex.Message);
            }
        }
    }
#pragma warning restore VSTHRD010

    /// <summary>
    /// Returns the project display name from a <see cref="Project"/> object, or a fallback.
    /// </summary>
    private static string GetProjectName(Project project)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        try { return project.Name ?? "(unnamed)"; }
        catch { return "(inaccessible)"; }
    }

    private static readonly Guid MiscellaneousFilesProjectGuid = new("{A2FE74E1-B743-11d0-AE1A-00A0C90FFFC3}");

    private static bool IsMiscellaneousFilesProject(IVsHierarchy hier)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        try
        {
            hier.GetGuidProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ProjectIDGuid, out var guid);
            return guid == MiscellaneousFilesProjectGuid;
        }
        catch
        {
            return true;
        }
    }

    private static string GetItemName(ProjectItem item)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        try { return item.Name ?? "(unknown)"; }
        catch { return "(unknown)"; }
    }
}
