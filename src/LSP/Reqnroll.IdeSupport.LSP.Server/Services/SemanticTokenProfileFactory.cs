namespace Reqnroll.IdeSupport.LSP.Server.Services;

/// <summary>
/// Creates the <see cref="ISemanticTokenProfile"/> appropriate for a given IDE,
/// identified by the <c>--ide</c> command-line argument passed to the LSP server process.
/// </summary>
/// <remarks>
/// The IDE identifier is passed by each IDE's glue component when it spawns the server.
/// Unknown or absent identifiers fall back to the Visual Studio profile so that a
/// debug run without <c>--ide</c> still produces useful output.
/// </remarks>
public static class SemanticTokenProfileFactory
{
    // ── Known IDE identifier strings ──────────────────────────────────────────
    // These constants are the canonical values used both here and by each IDE's
    // client-side glue (e.g. ReqnrollLanguageClient.cs for VS) when launching the server.

    /// <summary>Visual Studio 2022+ (VisualStudio.Extensibility / VSSDK).</summary>
    public const string VisualStudio = "visualstudio";

    /// <summary>Visual Studio Code (Language Client extension).</summary>
    public const string VsCode = "vscode";

    /// <summary>JetBrains Rider.</summary>
    public const string Rider = "rider";

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns an <see cref="ISemanticTokenProfile"/> for the given IDE identifier.
    /// </summary>
    /// <param name="ideIdentifier">
    /// The value of the <c>--ide</c> command-line argument, or <see langword="null"/>
    /// when the argument is absent.  Comparison is case-insensitive.
    /// </param>
    public static ISemanticTokenProfile Create(string? ideIdentifier) =>
        (ideIdentifier?.Trim().ToLowerInvariant()) switch
        {
            VisualStudio => new VisualStudioSemanticTokenProfile(),

            // Placeholder profiles: each IDE's profile will be introduced when the
            // client-side glue for that IDE is implemented.  They start as aliases of
            // the VS profile so the server works end-to-end from day one.
            VsCode => new VisualStudioSemanticTokenProfile(), // TODO: VsCodeSemanticTokenProfile
            Rider  => new VisualStudioSemanticTokenProfile(), // TODO: RiderSemanticTokenProfile

            // Unknown or absent --ide value — default to VS profile.
            _ => new VisualStudioSemanticTokenProfile(),
        };
}
