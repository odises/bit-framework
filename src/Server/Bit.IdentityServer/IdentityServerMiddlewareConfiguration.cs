﻿using Bit.Core.Contracts;
using Bit.Core.Extensions;
using Bit.Core.Models;
using Bit.IdentityServer.Contracts;
using Bit.IdentityServer.Implementations;
using Bit.Owin.Contracts;
using IdentityServer3.Core.Configuration;
using IdentityServer3.Core.Logging;
using IdentityServer3.Core.Services;
using Microsoft.Owin;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bit.IdentityServer
{
    public class IdentityServerMiddlewareConfiguration : IOwinMiddlewareConfiguration
    {
        public virtual AppEnvironment AppEnvironment { get; set; }
        public virtual IAppCertificatesProvider AppCertificatesProvider { get; set; }
        public virtual IDependencyManager DependencyManager { get; set; }
        public virtual IScopesProvider ScopesProvider { get; set; }
        public virtual IRedirectUriValidator RedirectUriValidator { get; set; }
        public virtual IEnumerable<IExternalIdentityProviderConfiguration> ExternalIdentityProviderConfigurations { get; set; }
        public virtual IEnumerable<IIdentityServerOptionsCustomizer> Customizers { get; set; }

        public virtual void Configure(IAppBuilder owinApp)
        {
            if (owinApp == null)
                throw new ArgumentNullException(nameof(owinApp));

            owinApp.Map("/core", coreApp =>
            {
                LogProvider.SetCurrentLogProvider(DependencyManager.Resolve<ILogProvider>());

                IdentityServerServiceFactory factory = new IdentityServerServiceFactory()
                    .UseInMemoryClients(DependencyManager.Resolve<IOAuthClientsProvider>().GetClients().ToArray())
                    .UseInMemoryScopes(ScopesProvider.GetScopes());

                IUserService ResolveUserService(IdentityServer3.Core.Services.IDependencyResolver resolver)
                {
                    OwinEnvironmentService owinEnv = resolver.Resolve<OwinEnvironmentService>();
                    IOwinContext owinContext = new OwinContext(owinEnv.Environment);
                    IUserService userService = owinContext.GetDependencyResolver().Resolve<IUserService>();
                    return userService;
                }

                factory.UserService = new Registration<IUserService>(ResolveUserService);

                IEventService ResolveEventService(IdentityServer3.Core.Services.IDependencyResolver resolver)
                {
                    OwinEnvironmentService owinEnv = resolver.Resolve<OwinEnvironmentService>();
                    IOwinContext owinContext = new OwinContext(owinEnv.Environment);
                    if (owinContext.TryGetDependencyResolver(out Core.Contracts.IDependencyResolver dependencyResolver))
                    {
                        IRequestInformationProvider requestInformationProvider = dependencyResolver.Resolve<IRequestInformationProvider>();
                        owinEnv.Environment["server.RemoteIpAddress"] = requestInformationProvider.ClientIp ?? "::1"; // some test hosts won't provide remote ip address request feature and idSrv requires it in event sevice decorator.
                        return dependencyResolver.Resolve<IEventService>();
                    }
                    else
                    {
                        return new FakeEventService { };
                    }
                }

                factory.EventService = new Registration<IEventService>(ResolveEventService);

                IViewService ResolveViewService(IdentityServer3.Core.Services.IDependencyResolver resolver)
                {
                    OwinEnvironmentService owinEnv = resolver.Resolve<OwinEnvironmentService>();
                    IOwinContext owinContext = new OwinContext(owinEnv.Environment);
                    return owinContext.GetDependencyResolver().Resolve<IViewService>();
                }

                factory.ViewService = new Registration<IViewService>(ResolveViewService);

                factory.RedirectUriValidator = new Registration<IRedirectUriValidator>(RedirectUriValidator);

                bool requireSslConfigValue = AppEnvironment.GetConfig(AppEnvironment.KeyValues.RequireSsl, defaultValueOnNotFound: AppEnvironment.KeyValues.RequireSslDefaultValue);

                string identityServerSiteName = AppEnvironment.GetConfig(AppEnvironment.KeyValues.IdentityServer.IdentityServerSiteName, $"{AppEnvironment.AppInfo.Name} Identity Server");

                IdentityServerOptions identityServerOptions = new IdentityServerOptions
                {
                    SiteName = identityServerSiteName,
                    SigningCertificate = AppCertificatesProvider.GetSingleSignOnCertificate(),
                    Factory = factory,
                    RequireSsl = requireSslConfigValue,
                    EnableWelcomePage = AppEnvironment.DebugMode == true,
                    IssuerUri = AppEnvironment.GetSsoIssuerName(),
                    CspOptions = new CspOptions
                    {
                        // Content security policy
                        Enabled = false
                    },
                    Endpoints = new EndpointOptions
                    {
                        EnableAccessTokenValidationEndpoint = true,
                        EnableAuthorizeEndpoint = true,
                        EnableCheckSessionEndpoint = true,
                        EnableClientPermissionsEndpoint = true,
                        EnableCspReportEndpoint = true,
                        EnableDiscoveryEndpoint = true,
                        EnableEndSessionEndpoint = true,
                        EnableIdentityTokenValidationEndpoint = true,
                        EnableIntrospectionEndpoint = true,
                        EnableTokenEndpoint = true,
                        EnableTokenRevocationEndpoint = true,
                        EnableUserInfoEndpoint = true
                    },
                    EventsOptions = new EventsOptions
                    {
                        RaiseErrorEvents = true,
                        RaiseFailureEvents = true
                    },
                    AuthenticationOptions = new AuthenticationOptions
                    {
                        IdentityProviders = ConfigureIdentityProviders
                    }
                };

                foreach (IIdentityServerOptionsCustomizer customizer in Customizers)
                {
                    customizer.Customize(identityServerOptions);
                }

                coreApp.UseIdentityServer(identityServerOptions);
            });
        }

        protected virtual void ConfigureIdentityProviders(IAppBuilder owinApp, string signInAsType)
        {
            foreach (IExternalIdentityProviderConfiguration externalIdentityProviderConfiguration in ExternalIdentityProviderConfigurations)
            {
                externalIdentityProviderConfiguration.ConfigureExternalIdentityProvider(owinApp, signInAsType);
            }
        }
    }
}
