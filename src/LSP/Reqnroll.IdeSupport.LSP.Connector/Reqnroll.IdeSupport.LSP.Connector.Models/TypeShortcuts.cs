using System.Collections.Generic;
using System.Linq;

namespace Reqnroll.IdeSupport.LSP.Connector.Models;

/// <summary>TypeShortcuts</summary>
public static class TypeShortcuts
{
    /// <summary>Gets or sets the reqnroll table type.</summary>
    public const string ReqnrollTableType = "Reqnroll.Table";
    /// <summary>Gets or sets the string type.</summary>
    public const string StringType = "System.String";
    /// <summary>Gets or sets the int32 type.</summary>
    public const string Int32Type = "System.Int32";

    /// <summary>Gets or sets the from shortcut.</summary>
    public static readonly Dictionary<string, string> FromShortcut = new()
    {
        {"s", StringType},
        {"c", typeof(char).FullName},
        {"b", typeof(bool).FullName},
        {"bt", typeof(byte).FullName},
        {"i", Int32Type},
        {"sh", typeof(short).FullName},
        {"l", typeof(long).FullName},
        {"f", typeof(float).FullName},
        {"d", typeof(double).FullName},
        {"m", typeof(decimal).FullName},
        {"st", ReqnrollTableType}
    };

    /// <summary>Gets or sets the from type.</summary>
    public static readonly Dictionary<string, string> FromType = FromShortcut.ToDictionary(p => p.Value, p => p.Key);
}
