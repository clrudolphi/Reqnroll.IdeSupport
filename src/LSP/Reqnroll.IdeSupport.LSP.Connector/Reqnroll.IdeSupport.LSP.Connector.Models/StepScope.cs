#nullable disable

namespace Reqnroll.IdeSupport.LSP.Connector.Models;

/// <summary>StepScope</summary>
public class StepScope
{
    /// <summary>Gets or sets the tag.</summary>
    public string Tag { get; set; }
    /// <summary>Gets or sets the feature title.</summary>
    public string FeatureTitle { get; set; }
    /// <summary>Gets or sets the scenario title.</summary>
    public string ScenarioTitle { get; set; }
    /// <summary>Gets or sets the error.</summary>
    public string Error { get; set; }

    #region Equality

    /// <summary>Gets or sets the equals.</summary>
    protected bool Equals(StepScope other) => string.Equals(Tag, other.Tag) &&
                                              string.Equals(FeatureTitle, other.FeatureTitle) &&
                                              string.Equals(ScenarioTitle, other.ScenarioTitle) &&
                                              string.Equals(Error, other.Error);

    /// <summary>Gets or sets the equals.</summary>
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((StepScope) obj);
    }

    /// <summary>Gets or sets the get hash code.</summary>
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Tag != null ? Tag.GetHashCode() : 0;
            hashCode = (hashCode * 397) ^ (FeatureTitle != null ? FeatureTitle.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (ScenarioTitle != null ? ScenarioTitle.GetHashCode() : 0);
            return hashCode;
        }
    }

    #endregion
}
