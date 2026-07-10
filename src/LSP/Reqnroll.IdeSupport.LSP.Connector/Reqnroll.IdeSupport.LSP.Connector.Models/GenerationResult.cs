#nullable disable

namespace Reqnroll.IdeSupport.LSP.Connector.Models;

/// <summary>GenerationResult</summary>
public class GenerationResult : ConnectorResult
{
    /// <summary>Gets or sets the feature file code behind.</summary>
    public FeatureFileCodeBehind FeatureFileCodeBehind { get; set; }
}
