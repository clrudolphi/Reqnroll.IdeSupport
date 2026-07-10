#nullable disable

using Reqnroll;
using Reqnroll.IdeSupport.Common.ProjectSystem.Settings;
using System.Text.RegularExpressions;

namespace Reqnroll.IdeSupport.Common.Configuration;

/// <summary>ReqnrollConfiguration</summary>
public class ReqnrollConfiguration
{
    /// <summary>Gets or sets the is reqnroll project.</summary>
    public bool? IsReqnrollProject { get; set; }

    /// <summary>Gets or sets the version.</summary>
    public string Version { get; set; }
    /// <summary>Gets or sets the config file path.</summary>
    public string ConfigFilePath { get; set; }
    /// <summary>Gets or sets the traits.</summary>
    public ReqnrollProjectTraits[] Traits { get; set; } = new ReqnrollProjectTraits[0];

    private void FixEmptyContainers()
    {
        Traits = Traits ?? new ReqnrollProjectTraits[0];
    }

    /// <summary>Gets or sets the check configuration.</summary>
    public void CheckConfiguration()
    {
        FixEmptyContainers();

        if (Version != null && !Regex.IsMatch(Version, @"^(?:\.?[0-9]+){2,}(?:\-[\-a-z0-9]*)?$"))
            throw new DeveroomConfigurationException("'reqnroll/version' was not in a correct format");
    }

    #region Equality

    /// <summary>Gets or sets the equals.</summary>
    protected bool Equals(ReqnrollConfiguration other) => IsReqnrollProject == other.IsReqnrollProject &&
                                                          string.Equals(Version, other.Version) &&
                                                          string.Equals(ConfigFilePath, other.ConfigFilePath) &&
                                                          Equals(Traits, other.Traits);

    /// <summary>Gets or sets the equals.</summary>
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((ReqnrollConfiguration) obj);
    }

    /// <summary>Gets or sets the get hash code.</summary>
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = IsReqnrollProject.GetHashCode();
            hashCode = (hashCode * 397) ^ (Version != null ? Version.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (ConfigFilePath != null ? ConfigFilePath.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (Traits != null ? Traits.GetHashCode() : 0);
            return hashCode;
        }
    }

    #endregion
}
