using System;
using System.IO;

namespace Reqnroll.IDE.Common.Analytics;

public interface IUserUniqueIdStore
{
    string GetUserId();
}

