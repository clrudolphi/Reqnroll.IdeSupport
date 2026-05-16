using System;

namespace Reqnroll.IDE.Common.Configuration;

[AttributeUsage(AttributeTargets.Property)]
public class EditorConfigSettingAttribute : Attribute
{
    public EditorConfigSettingAttribute(string editorConfigSettingName)
    {
        EditorConfigSettingName = editorConfigSettingName;
    }

    public string EditorConfigSettingName { get; }
}
