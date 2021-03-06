﻿using Bit.Core.Models;
using Bit.Hangfire.Contracts;
using Hangfire;
using Hangfire.Logging;
using System.Collections.Generic;
using System.Linq;

namespace Bit.Hangfire.Implementations
{
    public abstract class JobSchedulerBaseBackendConfiguration<TStorage> : IJobSchedulerBackendConfiguration
        where TStorage : JobStorage
    {
        public virtual JobActivator JobActivator { get; set; }

        public virtual ILogProvider LogProvider { get; set; }

        public virtual IEnumerable<IHangfireOptionsCustomizer> Customizers { get; set; }

        protected virtual BackgroundJobServer BackgroundJobServer { get; set; }

        protected abstract TStorage BuildStorage();

        public virtual void Init()
        {
            var storage = BuildStorage();

            JobStorage.Current = storage;

            GlobalConfiguration.Configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseStorage(storage)
                .UseLogProvider(LogProvider);

            if (GlobalJobFilters.Filters.Any(f => f.Instance is AutomaticRetryAttribute))
            {
                GlobalConfiguration.Configuration.UseFilter(new DefaultAutomaticRetryAttribute { });
                GlobalJobFilters.Filters.Remove(GlobalJobFilters.Filters.ExtendedSingle("Finding automatic retry job filter attribute to remove it from global job filters", f => f.Instance is AutomaticRetryAttribute).Instance);
            }

            BackgroundJobServerOptions options = new BackgroundJobServerOptions
            {
                Activator = JobActivator
            };

            foreach (IHangfireOptionsCustomizer customizer in Customizers)
            {
                customizer.Customize(GlobalConfiguration.Configuration, options, storage);
            }

            BackgroundJobServer = new BackgroundJobServer(options, storage);
        }

        public virtual void Dispose()
        {
            BackgroundJobServer?.Stop(force: true);
            BackgroundJobServer?.Dispose();
        }
    }
}
