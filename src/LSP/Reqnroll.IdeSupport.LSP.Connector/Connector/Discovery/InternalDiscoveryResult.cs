namespace ReqnrollConnector.Discovery;

public record InternalDiscoveryResult(
    Reqnroll.IdeSupport.LSP.Connector.Models.StepDefinition[] StepDefinitions,
    Reqnroll.IdeSupport.LSP.Connector.Models.Hook[] Hooks,
    IDictionary<string, string> SourceFiles,
    IDictionary<string, string> TypeNames,
    string[] GenericBindingErrors,
    string[] TypeLoadErrors
);
