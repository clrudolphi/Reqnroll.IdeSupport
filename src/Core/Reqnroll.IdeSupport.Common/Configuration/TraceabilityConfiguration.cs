using System;
using System.Linq;

namespace Reqnroll.IdeSupport.Common.Configuration;

/// <summary>TraceabilityConfiguration</summary>
public class TraceabilityConfiguration
{
    /// <summary>Gets or sets the tag links.</summary>
    public TagLinkConfiguration[] TagLinks { get; set; } = new TagLinkConfiguration[0];

    private void FixEmptyContainers()
    {
        TagLinks = TagLinks ?? new TagLinkConfiguration[0];
    }

    /// <summary>Gets or sets the check configuration.</summary>
    public void CheckConfiguration()
    {
        FixEmptyContainers();

        foreach (var tagLinkConfiguration in TagLinks) tagLinkConfiguration.CheckConfiguration();
    }

    #region Equality

    /// <summary>Gets or sets the equals.</summary>
    protected bool Equals(TraceabilityConfiguration other) => Equals(TagLinks, other.TagLinks);

    /// <summary>Gets or sets the equals.</summary>
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((TraceabilityConfiguration) obj);
    }

    /// <summary>Gets or sets the get hash code.</summary>
    public override int GetHashCode() => TagLinks != null ? TagLinks.GetHashCode() : 0;

    #endregion
}
