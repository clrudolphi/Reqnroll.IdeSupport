using Reqnroll.IdeSupport.LSP.Core.Documents;

namespace Reqnroll.IdeSupport.LSP.Server.Features.TextSync;

/// <summary>DocumentBufferExtensions</summary>
public static class DocumentBufferExtensions
{
    /// <summary>Gets or sets the to gherkin text snapshot.</summary>
    public static IGherkinTextSnapshot ToGherkinTextSnapshot(this DocumentBuffer buffer)
            => new LspTextSnapshot(buffer.Uri.ToString(), buffer.Version ?? 0, buffer.Text);
}
