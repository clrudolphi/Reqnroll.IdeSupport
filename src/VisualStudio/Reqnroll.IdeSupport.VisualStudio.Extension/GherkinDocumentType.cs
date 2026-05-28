using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Editor;
using Microsoft.VisualStudio.Extensibility.LanguageServer;

namespace Reqnroll.IdeSupport.VisualStudio.Extension;

internal static class GherkinDocumentType
{
    [VisualStudioContribution]
    internal static DocumentTypeConfiguration GherkinDocument => new("reqnroll-gherkin")
    {
        FileExtensions = new[] { ".feature" },
        BaseDocumentType = LanguageServerProvider.LanguageServerBaseDocumentType,
    };
}
