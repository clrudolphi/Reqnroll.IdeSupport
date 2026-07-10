using System;
using System.IO;

namespace Reqnroll.IdeSupport.Common.Telemetry;

public interface IUserUniqueIdStore
{
    string GetUserId();
}

