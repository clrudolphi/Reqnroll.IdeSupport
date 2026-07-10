#nullable disable

using System.Collections.Generic;

namespace Reqnroll.IdeSupport.VisualStudio.ProjectSystem;

/// <summary>IDeveroomErrorListServices</summary>
public interface IDeveroomErrorListServices
{
    /// <summary>Gets or sets the clear errors.</summary>
    void ClearErrors(DeveroomUserErrorCategory category);
    /// <summary>Gets or sets the add errors.</summary>
    void AddErrors(IEnumerable<DeveroomUserError> errors);
}

/// <summary>DeveroomUserErrorCategory</summary>
public enum DeveroomUserErrorCategory
{
    /// <summary>Gets or sets the discovery.</summary>
    Discovery
}

/// <summary>DeveroomUserError</summary>
public class DeveroomUserError
{
    /// <summary>Gets or sets the message.</summary>
    public string Message { get; set; }
    //public SourceLocation SourceLocation { get; set; }
    /// <summary>Gets or sets the category.</summary>
    public DeveroomUserErrorCategory Category { get; set; }
}
