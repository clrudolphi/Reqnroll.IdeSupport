namespace Reqnroll.IdeSupport.Common.Configuration;

/// <summary>
/// Returns the caller-supplied default for every key. Used when no .editorconfig applies.
/// </summary>
public sealed class NullEditorConfigOptions : IEditorConfigOptions
{
    /// <summary>Gets or sets the instance.</summary>
    public static readonly NullEditorConfigOptions Instance = new();

    private NullEditorConfigOptions() { }

    /// <summary>Gets or sets the get option<tresult>.</summary>
    public TResult GetOption<TResult>(string key, TResult defaultValue) => defaultValue;
    /// <summary>Gets or sets the get bool option.</summary>
    public bool GetBoolOption(string key, bool defaultValue) => defaultValue;
}
