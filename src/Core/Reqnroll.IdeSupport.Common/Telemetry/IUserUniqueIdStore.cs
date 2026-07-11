
namespace Reqnroll.IdeSupport.Common.Telemetry;

/// <summary>IUserUniqueIdStore</summary>
public interface IUserUniqueIdStore
{
    /// <summary>Returns the persisted unique identifier for the current user, creating one if none exists yet.</summary>
    string GetUserId();
}

