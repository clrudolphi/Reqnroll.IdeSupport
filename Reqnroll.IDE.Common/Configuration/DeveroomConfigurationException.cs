using System;

namespace Reqnroll.IDE.Common.Configuration;

public class DeveroomConfigurationException : Exception
{
    public DeveroomConfigurationException()
    {
    }

    public DeveroomConfigurationException(string message) : base(message)
    {
    }

    public DeveroomConfigurationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
