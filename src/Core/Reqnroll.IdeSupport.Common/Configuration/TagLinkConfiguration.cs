#nullable disable

using System;
using System.Text.RegularExpressions;

namespace Reqnroll.IdeSupport.Common.Configuration;

/// <summary>TagLinkConfiguration</summary>
public class TagLinkConfiguration
{
    /// <summary>Gets or sets the tag pattern.</summary>
    public string TagPattern { get; set; }
    /// <summary>Gets or sets the url template.</summary>
    public string UrlTemplate { get; set; }

    internal Regex ResolvedTagPattern { get; private set; }

    private void FixEmptyContainers()
    {
        //nop;
    }

    /// <summary>Gets or sets the check configuration.</summary>
    public void CheckConfiguration()
    {
        FixEmptyContainers();

        if (string.IsNullOrEmpty(TagPattern))
            throw new DeveroomConfigurationException("'traceability/tagLinks[]/tagPattern' must be specified");
        if (string.IsNullOrEmpty(UrlTemplate))
            throw new DeveroomConfigurationException("'traceability/tagLinks[]/urlTemplate' must be specified");

        try
        {
            ResolvedTagPattern = new Regex("^" + TagPattern.TrimStart('^').TrimEnd('$') + "$");
        }
        catch (Exception e)
        {
            throw new DeveroomConfigurationException(
                $"Invalid regular expression '{TagPattern}' was specified as 'traceability/tagLinks[]/tagPattern': {e.Message}");
        }
    }

    #region Equality

    /// <summary>Gets or sets the equals.</summary>
    protected bool Equals(TagLinkConfiguration other) => string.Equals(TagPattern, other.TagPattern) &&
                                                         string.Equals(UrlTemplate, other.UrlTemplate) &&
                                                         Equals(ResolvedTagPattern, other.ResolvedTagPattern);

    /// <summary>Gets or sets the equals.</summary>
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((TagLinkConfiguration) obj);
    }

    /// <summary>Gets or sets the get hash code.</summary>
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = TagPattern != null ? TagPattern.GetHashCode() : 0;
            hashCode = (hashCode * 397) ^ (UrlTemplate != null ? UrlTemplate.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (ResolvedTagPattern != null ? ResolvedTagPattern.GetHashCode() : 0);
            return hashCode;
        }
    }

    #endregion
}
