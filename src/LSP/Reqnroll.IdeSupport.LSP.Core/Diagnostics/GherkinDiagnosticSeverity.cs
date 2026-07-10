namespace Reqnroll.IdeSupport.LSP.Core.Diagnostics;

/// <summary>
/// Protocol-agnostic severity for a <see cref="GherkinDiagnostic"/>.
/// The server layer maps these values to <c>OmniSharp.Extensions.LanguageServer.Protocol.Models.DiagnosticSeverity</c>.
/// </summary>
public enum GherkinDiagnosticSeverity
{
    /// <summary>Gets or sets the error.</summary>
    Error   = 1,
    /// <summary>Gets or sets the warning.</summary>
    Warning = 2
}
