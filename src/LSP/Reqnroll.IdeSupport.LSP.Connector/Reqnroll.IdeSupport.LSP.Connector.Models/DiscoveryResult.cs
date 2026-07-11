#nullable disable
using System;
using System.Collections.Generic;

namespace Reqnroll.IdeSupport.LSP.Connector.Models;

/// <summary>DiscoveryResult</summary>
public class DiscoveryResult : ConnectorResult
{
    /// <summary>Gets or sets the step definitions.</summary>
    public StepDefinition[] StepDefinitions { get; set; } = Array.Empty<StepDefinition>();
    /// <summary>Gets or sets the hooks.</summary>
    public Hook[] Hooks { get; set; } = Array.Empty<Hook>();
    /// <summary>Gets or sets the source files.</summary>
    public Dictionary<string, string> SourceFiles { get; set; }
    /// <summary>Gets or sets the type names.</summary>
    public Dictionary<string, string> TypeNames { get; set; }
    /// <summary>Gets or sets the generic binding errors.</summary>
    public string[] GenericBindingErrors { get; set; }
}
