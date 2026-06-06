using OmniSharp.Extensions.LanguageServer.Protocol;
using Reqnroll.IdeSupport.LSP.Core.Editor.Services.Parsing.GherkinDocuments;

namespace Reqnroll.IdeSupport.LSP.Server.Services
{
    public interface IGherkinDocumentTaggerService
    {
        Task<IReadOnlyCollection<DeveroomTag>> ParseAsync(DocumentUri uri, int? version);

        /// <summary>
        /// Parses <paramref name="text"/> as the content of <paramref name="uri"/> and stores the
        /// resulting binding match set, without requiring a prior entry in the document buffer.
        /// Used for workspace-wide scans of feature files that are not currently open in the editor
        /// (e.g. the initial scan triggered on startup by a full binding-registry replacement).
        /// The document buffer is not updated; open-file semantics are unaffected.
        /// </summary>
        Task ScanClosedFileAsync(DocumentUri uri, string text);
    }
}
