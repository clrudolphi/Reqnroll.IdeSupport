namespace Reqnroll.IdeSupport.Common.Configuration;

/// <summary>
/// No-op provider that always returns <see cref="NullEditorConfigOptions"/>. Used in contexts
/// where .editorconfig lookup is unavailable (tests, non-LSP hosts).
/// </summary>
public sealed class NullEditorConfigOptionsProvider : IEditorConfigOptionsProvider
{
    /// <summary>The shared singleton instance.</summary>
    public static readonly NullEditorConfigOptionsProvider Instance = new();

    private NullEditorConfigOptionsProvider() { }

    /// <summary>Returns the shared no-op <see cref="NullEditorConfigOptions"/> regardless of the given path.</summary>
    public IEditorConfigOptions GetEditorConfigOptionsByPath(string filePath)
        => NullEditorConfigOptions.Instance;

    /// <summary>No-op: there is no cache to invalidate.</summary>
    public void InvalidateCache(string editorConfigFilePath) { }
}
