using System;

namespace Reqnroll.IdeSupport.Common.Configuration;

/// <summary>DeveroomConfigurationException</summary>
public class DeveroomConfigurationException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="DeveroomConfigurationException"/> class.</summary>
    public DeveroomConfigurationException()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="DeveroomConfigurationException"/> class.</summary>
    public DeveroomConfigurationException(string message) : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="DeveroomConfigurationException"/> class.</summary>
    public DeveroomConfigurationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
