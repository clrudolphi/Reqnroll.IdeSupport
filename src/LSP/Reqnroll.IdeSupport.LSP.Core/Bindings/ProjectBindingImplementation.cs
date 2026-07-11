using Reqnroll.IdeSupport.LSP.Core.Documents;

namespace Reqnroll.IdeSupport.LSP.Core.Bindings;

/// <summary>Identifies the .NET method a step definition or hook binding maps to: its name, parameter types, and source location.</summary>
public class ProjectBindingImplementation
{
    private static readonly string[] EmptyParameterTypes = Array.Empty<string>();

    /// <summary>Creates a binding implementation descriptor.</summary>
    public ProjectBindingImplementation(string method, string[]? parameterTypes, SourceLocation sourceLocation)
    {
        Method = method;
        ParameterTypes = parameterTypes ?? EmptyParameterTypes;
        SourceLocation = sourceLocation;
    }

    /// <summary>The bound method's name/identity as reported by discovery.</summary>
    public string Method { get; } //TODO: Name, URI, SourceType?
    /// <summary>The resolved .NET parameter types of the bound method, in order.</summary>
    public string[] ParameterTypes { get; }
    /// <summary>The source location of the bound method, when known.</summary>
    public SourceLocation? SourceLocation { get; }

    /// <summary>Returns the method name for diagnostics/logging.</summary>
    public override string ToString() => Method;
}
