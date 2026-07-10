namespace Reqnroll.IdeSupport.Common.Configuration;

/// <summary>
/// No-op provider that always returns <see cref="NullEditorConfigOptions"/>. Used in contexts
/// where .editorconfig lookup is unavailable (tests, non-LSP hosts).
/// </summary>
public sealed class NullEditorConfigOptionsProvider : IEditorConfigOptionsProvider
{
    /// <summary>Gets or sets the instance.</summary>
    public static readonly NullEditorConfigOptionsProvider Instance = new();

    private NullEditorConfigOptionsProvider() { }

    /// <summary>Gets or sets the get editor config options by path.</summary>
    public IEditorConfigOptions GetEditorConfigOptionsByPath(string filePath)
        => NullEditorConfigOptions.Instance;

    /// <summary>Gets or sets the invalidate cache.</summary>
    public void InvalidateCache(string editorConfigFilePath) { }
}
