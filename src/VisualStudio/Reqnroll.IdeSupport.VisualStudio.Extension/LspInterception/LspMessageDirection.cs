namespace Reqnroll.IdeSupport.VisualStudio.Extension.LspInterception;

/// <summary>
/// Indicates the direction a JSON-RPC message is flowing relative to the LSP server.
/// </summary>
internal enum LspMessageDirection
{
    /// <summary>The message is being sent from VS to the LSP server.</summary>
    Send,

    /// <summary>The message was received from the LSP server and is heading to VS.</summary>
    Receive,
}
