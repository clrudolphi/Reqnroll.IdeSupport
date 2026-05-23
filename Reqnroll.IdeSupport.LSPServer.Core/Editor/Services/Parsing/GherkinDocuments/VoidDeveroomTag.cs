using Reqnroll.IDESupport.LSPServer.Core.Document;

namespace Reqnroll.IdeSupport.LSPServer.Core.Editor.Services.Parsing.GherkinDocuments;

public record VoidDeveroomTag : DeveroomTag
{
    public static VoidDeveroomTag Instance = new();

    private VoidDeveroomTag() : base("Void", GherkinRange.Empty, new object())
    {
    }

    internal override DeveroomTag AddChild(DeveroomTag childTag)
    {
        childTag.ParentTag = this;
        return childTag;
    }
}
