namespace Reqnroll.IdeSupport.Common.Configuration;

/// <summary>BindingDiscoveryConfiguration</summary>
public class BindingDiscoveryConfiguration
{
    /// <summary>Gets or sets the connector path.</summary>
    public string? ConnectorPath { get; set; } = null;

    private void FixEmptyContainers()
    {
    }

    /// <summary>Gets or sets the check configuration.</summary>
    public void CheckConfiguration()
    {
        FixEmptyContainers();
    }

    #region Equality

    /// <summary>Gets or sets the equals.</summary>
    protected bool Equals(BindingDiscoveryConfiguration other)
    {
        return ConnectorPath == other.ConnectorPath;
    }

    /// <summary>Gets or sets the equals.</summary>
    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((BindingDiscoveryConfiguration)obj);
    }

    // ReSharper disable NonReadonlyMemberInGetHashCode
    /// <summary>Gets or sets the get hash code.</summary>
    public override int GetHashCode()
    {
        return (ConnectorPath != null ? ConnectorPath.GetHashCode() : 0);
    }
    // ReSharper restore NonReadonlyMemberInGetHashCode

    #endregion
}