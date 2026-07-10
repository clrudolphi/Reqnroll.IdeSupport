using System;

namespace Reqnroll.IdeSupport.Common.Configuration;

[AttributeUsage(AttributeTargets.Property)]
/// <summary>EditorConfigSettingAttribute</summary>
public class EditorConfigSettingAttribute : Attribute
{
    /// <summary>Initializes a new instance of the <see cref="EditorConfigSettingAttribute"/> class.</summary>
    public EditorConfigSettingAttribute(string editorConfigSettingName)
    {
        EditorConfigSettingName = editorConfigSettingName;
    }

    /// <summary>Gets or sets the editor config setting name.</summary>
    public string EditorConfigSettingName { get; }
}
