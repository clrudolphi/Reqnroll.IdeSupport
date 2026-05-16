using Reqnroll.IDE.Common.Configuration;

namespace Reqnroll.IDE.Common.ProjectSystem.Configuration;

internal record ConfigCache(DeveroomConfiguration Configuration, ConfigSource[] ConfigSources);
