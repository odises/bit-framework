﻿using Hangfire;
using Hangfire.MemoryStorage;
using System;
using System.Reflection;

namespace Bit.Hangfire.Implementations
{
    public class JobSchedulerInMemoryBackendConfiguration : JobSchedulerBaseBackendConfiguration<MemoryStorage>
    {
        protected override MemoryStorage BuildStorage()
        {
            MemoryStorage storage = new MemoryStorage();

            typeof(BackgroundJob)
                .GetProperty("ClientFactory", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, new Func<IBackgroundJobClient>(() => new BackgroundJobClient()));

            return storage;
        }
    }
}
