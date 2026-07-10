namespace Reqnroll.IdeSupport.VisualStudio.ProjectSystem;

/// <summary>IDeveroomOutputPaneServices</summary>
public interface IDeveroomOutputPaneServices
{
    /// <summary>Gets or sets the write line.</summary>
    void WriteLine(string text);
    /// <summary>Gets or sets the send write line.</summary>
    void SendWriteLine(string text);
    /// <summary>Gets or sets the activate.</summary>
    void Activate();
}
