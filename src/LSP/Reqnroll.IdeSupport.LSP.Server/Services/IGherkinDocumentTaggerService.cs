using OmniSharp.Extensions.LanguageServer.Protocol;
using Reqnroll.IdeSupport.LSP.Core.Editor.Services.Parsing.GherkinDocuments;

namespace Reqnroll.IdeSupport.LSP.Server.Services
{
    public interface IGherkinDocumentTaggerService
    {
        event EventHandler<GherkinDocumentTagsChangedEventArgs> GherkinDocumentTagsChanged;

        Task<IReadOnlyCollection<DeveroomTag>> GetTagsAsync(DocumentUri uri, int version);
        Task OnDocumentChangedAsync(DocumentUri uri, int? version);
        Task OnDocumentClosedAsync(DocumentUri uri);
    }
}