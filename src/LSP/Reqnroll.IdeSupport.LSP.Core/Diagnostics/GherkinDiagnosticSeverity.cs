namespace Reqnroll.IdeSupport.LSP.Core.Diagnostics;

/// <summary>
/// Protocol-agnostic severity for a <see cref="GherkinDiagnostic"/>.
/// The server layer maps these values to <c>OmniSharp.Extensions.LanguageServer.Protocol.Models.DiagnosticSeverity</c>.
/// </summary>
public enum GherkinDiagnosticSeverity
{
    /// <summary>A binding or parse problem that prevents correct execution, e.g. an undefined or ambiguous step.</summary>
    Error   = 1,
    /// <summary>A non-blocking issue that does not prevent execution, e.g. a style or formatting concern.</summary>
    Warning = 2
}
