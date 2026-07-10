#nullable disable
using Reqnroll;
using System;
using System.Linq;

namespace Reqnroll.IdeSupport.Common.ProjectSystem;

/// <summary>TargetFrameworkMoniker</summary>
public class TargetFrameworkMoniker
{
    /// <summary>Gets or sets the net core platform.</summary>
    public const string NetCorePlatform = ".NETCoreApp";
    /// <summary>Gets or sets the net framework platform.</summary>
    public const string NetFrameworkPlatform = ".NETFramework";
    private const string NetCoreShortValuePrefix = "netcoreapp";
    private const string NetFrameworkShortValuePrefix = "net";
    private const string Net5ShortValuePrefix = "net5";
    private const string Net6ShortValuePrefix = "net6";
    private const string Net7ShortValuePrefix = "net7";
    private const string Net8ShortValuePrefix = "net8";
    private const string Net9ShortValuePrefix = "net9";
    private const string Net10ShortValuePrefix = "net10";

    private TargetFrameworkMoniker(string value)
    {
        Value = value;

        if (value != null)
        {
            var parts = value.Split(',');
            Platform = parts[0].Trim();
            const string versionPartPrefix = "Version=v";
            var versionPart = parts.FirstOrDefault(p => p.StartsWith(versionPartPrefix));
            if (versionPart != null)
            {
                var versionString = versionPart.Substring(versionPartPrefix.Length);
                if (Version.TryParse(versionString, out var version))
                    Version = version;
            }
        }
    }

    // e.g.
    // * .NET 8:    .NETCoreApp,Version=v8.0
    // * .NET 7:    .NETCoreApp,Version=v7.0
    // * .NET 6:    .NETCoreApp,Version=v6.0
    // * .NET 5:    .NETCoreApp,Version=v5.0
    // * .NET Core: .NETCoreApp,Version=v2.1
    // * .NET Fw:   .NETFramework,Version=v4.5.2
    /// <summary>Gets or sets the value.</summary>
    public string Value { get; }

    /// <summary>Gets or sets the platform.</summary>
    public string Platform { get; }
    /// <summary>Gets or sets the version.</summary>
    public Version Version { get; }

    /// <summary>Gets or sets the has version.</summary>
    public bool HasVersion => Version != null;
    /// <summary>Gets or sets the is net core.</summary>
    public bool IsNetCore => NetCorePlatform.Equals(Platform);
    /// <summary>Gets or sets the is net framework.</summary>
    public bool IsNetFramework => NetFrameworkPlatform.Equals(Platform);

    /// <summary>Gets or sets the create.</summary>
    public static TargetFrameworkMoniker Create(string value) =>
        value == null ? null : new TargetFrameworkMoniker(value);

    /// <summary>Gets or sets the create from short name.</summary>
    public static TargetFrameworkMoniker CreateFromShortName(string shortValue)
    {
        var value = shortValue;
        if (shortValue.StartsWith(NetCoreShortValuePrefix))
        {
            value = $".NETCoreApp,Version=v{shortValue.Substring(NetCoreShortValuePrefix.Length)}";
        }
        else if (shortValue.StartsWith(Net5ShortValuePrefix))
        {
            value = $".NETCoreApp,Version=v{shortValue.Substring(Net5ShortValuePrefix.Length - 1)}";
        }
        else if (shortValue.StartsWith(Net6ShortValuePrefix))
        {
            value = $".NETCoreApp,Version=v{shortValue.Substring(Net6ShortValuePrefix.Length - 1)}";
        }
        else if (shortValue.StartsWith(Net7ShortValuePrefix))
        {
            value = $".NETCoreApp,Version=v{shortValue.Substring(Net7ShortValuePrefix.Length - 1)}";
        }
        else if (shortValue.StartsWith(Net8ShortValuePrefix))
        {
            value = $".NETCoreApp,Version=v{shortValue.Substring(Net8ShortValuePrefix.Length - 1)}";
        }
        else if (shortValue.StartsWith(Net9ShortValuePrefix))
        {
            value = $".NETCoreApp,Version=v{shortValue.Substring(Net9ShortValuePrefix.Length - 1)}";
        }
        else if (shortValue.StartsWith(Net10ShortValuePrefix))
        {
            value = $".NETCoreApp,Version=v{shortValue.Substring(Net10ShortValuePrefix.Length - 2)}";
        }
        else if (shortValue.StartsWith(NetFrameworkShortValuePrefix))
        {
            if (shortValue.Length == 5)
                value =
                    $".NETFramework,Version=v{shortValue[NetFrameworkShortValuePrefix.Length]}.{shortValue[NetFrameworkShortValuePrefix.Length + 1]}";
            else
                value =
                    $".NETFramework,Version=v{shortValue[NetFrameworkShortValuePrefix.Length]}.{shortValue[NetFrameworkShortValuePrefix.Length + 1]}.{shortValue[NetFrameworkShortValuePrefix.Length + 2]}";
        }

        return Create(value);
    }

    /// <summary>Gets or sets the to string.</summary>
    public override string ToString() => Value;

    // e.g netcoreapp2.1 or net452
    /// <summary>Gets or sets the to short string.</summary>
    public string ToShortString()
    {
        if (IsNetCore && HasVersion)
            return NetCoreShortValuePrefix + Version;
        if (IsNetFramework && HasVersion)
            return NetFrameworkShortValuePrefix + Version.ToString().Replace(".", "");
        return Value;
    }

    #region Equality

    /// <summary>Gets or sets the equals.</summary>
    protected bool Equals(TargetFrameworkMoniker other) => Value == other.Value;

    /// <summary>Gets or sets the equals.</summary>
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((TargetFrameworkMoniker) obj);
    }

    /// <summary>Gets or sets the get hash code.</summary>
    public override int GetHashCode() => Value.GetHashCode();

    /// <summary>Gets or sets the operator ==.</summary>
    public static bool operator ==(TargetFrameworkMoniker left, TargetFrameworkMoniker right) => Equals(left, right);

    /// <summary>Gets or sets the operator !=.</summary>
    public static bool operator !=(TargetFrameworkMoniker left, TargetFrameworkMoniker right) => !Equals(left, right);

    #endregion
}
