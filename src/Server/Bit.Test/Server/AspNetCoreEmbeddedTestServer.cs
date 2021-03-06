﻿using System.Net.Http;
using Bit.OwinCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using OpenQA.Selenium.Remote;
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Bit.Test.Server
{
    public class AspNetCoreEmbeddedTestServer : TestServerBase
    {
        private TestServer _server;

        public override void Initialize(string uri)
        {
            base.Initialize(uri);

            _server = new TestServer(BitWebHost.CreateDefaultBuilder(Array.Empty<string>())
                .UseUrls(uri)
                .UseStartup<AutofacAspNetCoreAppStartup>());
        }

        public override void Dispose()
        {
#if DotNetCore
            _server.Services.GetRequiredService<IHostApplicationLifetime>().StopApplication();
#endif
            _server.Dispose();
        }

        protected override HttpMessageHandler GetHttpMessageHandler()
        {
            return _server.CreateHandler();
        }

        public override RemoteWebDriver BuildWebDriver(RemoteWebDriverOptions options = null)
        {
            throw new NotSupportedException();
        }
    }
}
