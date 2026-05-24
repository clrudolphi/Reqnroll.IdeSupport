using System;

namespace Reqnroll.IdeSupport.Common.Configuration;

[AttributeUsage(AttributeTargets.Property)]
public class EditorConfigSettingAttribute : Attribute
{
    public EditorConfigSettingAttribute(string editorConfigSettingName)
    {
        EditorConfigSettingName = editorConfigSettingName;
    }

    public string EditorConfigSettingName { get; }
}
