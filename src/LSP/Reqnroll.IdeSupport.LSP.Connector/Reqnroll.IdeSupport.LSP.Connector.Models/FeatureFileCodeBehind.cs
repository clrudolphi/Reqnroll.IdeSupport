#nullable disable

namespace Reqnroll.IdeSupport.LSP.Connector.Models;

/// <summary>FeatureFileCodeBehind</summary>
public class FeatureFileCodeBehind
{
    /// <summary>Gets or sets the feature file path.</summary>
    public string FeatureFilePath { get; set; }
    /// <summary>Gets or sets the file path.</summary>
    public string FilePath { get; set; }
    /// <summary>Gets or sets the content.</summary>
    public string Content { get; set; }
}
