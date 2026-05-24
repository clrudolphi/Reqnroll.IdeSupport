using System;
using System.IO;

namespace Reqnroll.IdeSupport.Common.Analytics;

public interface IUserUniqueIdStore
{
    string GetUserId();
}

