using Reqnroll.IdeSupport.Common.Configuration;

namespace Reqnroll.IdeSupport.Common.ProjectSystem.Configuration;

internal record ConfigCache(DeveroomConfiguration Configuration, ConfigSource[] ConfigSources);
