using Reqnroll.IdeSupport.LSP.Core.Documents;

namespace Reqnroll.IdeSupport.LSP.Core.Parsing.Gherkin;

/// <summary>
/// A null-object <see cref="DeveroomTag"/> used as a root/placeholder parent when no real tag
/// is available, so callers can walk <c>ParentTag</c> chains without null-checking.
/// </summary>
public record VoidDeveroomTag : DeveroomTag
{
    /// <summary>The single shared instance of this null-object tag.</summary>
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
