using System;

namespace Reqnroll.IdeSupport.Common.Configuration;

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
