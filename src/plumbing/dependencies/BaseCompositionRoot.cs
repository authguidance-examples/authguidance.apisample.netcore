﻿namespace SampleApi.Host.Startup
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Caching.Distributed;
    using Microsoft.Extensions.DependencyInjection;
    using SampleApi.Plumbing.Claims;
    using SampleApi.Plumbing.Configuration;
    using SampleApi.Plumbing.Logging;
    using SampleApi.Plumbing.OAuth;
    using SampleApi.Plumbing.Security;
    using SampleApi.Plumbing.Utilities;

    /*
     * A class to manage composing core API behaviour
     */
    public sealed class BaseCompositionRoot
    {
        private OAuthConfiguration oauthConfiguration;
        private CustomClaimsProvider customClaimsProvider;
        private LoggingConfiguration loggingConfiguration;
        private LoggerFactory loggerFactory;
        private HttpProxy httpProxy;
        private IServiceCollection services;

        /*
         * Indicate that we're using OAuth and receive the configuration
         */
        public BaseCompositionRoot UseOAuth(OAuthConfiguration oauthConfiguration)
        {
            this.oauthConfiguration = oauthConfiguration;
            return this;
        }

        /*
         * Receive an object to manage processing claims
         */
        public BaseCompositionRoot WithCustomClaimsProvider(CustomClaimsProvider customClaimsProvider)
        {
            this.customClaimsProvider = customClaimsProvider;
            return this;
        }

        /*
         * Receive the logging configuration so that we can create objects related to logging and error handling
         */
        public BaseCompositionRoot WithLogging(LoggingConfiguration loggingConfiguration, ILoggerFactory loggerFactory)
        {
            this.loggingConfiguration = loggingConfiguration;
            this.loggerFactory = (LoggerFactory)loggerFactory;
            return this;
        }

        /*
         * Store an object to manage HTTP debugging
         */
        public BaseCompositionRoot WithProxyConfiguration(bool enabled, string url)
        {
            this.httpProxy = new HttpProxy(enabled, url);
            return this;
        }

        /*
         * Store an ASP.Net core services reference which we will update later
         */
        public BaseCompositionRoot WithServices(IServiceCollection services)
        {
            this.services = services;
            return this;
        }

        /*
         * Prepare and register objects
         */
        public void Register()
        {
            using (var container = this.services.BuildServiceProvider())
            {
                // Register dependencies for logging and error handling
                this.RegisterBaseDependencies();

                // Register dependencies for OAuth processing
                if (this.oauthConfiguration != null)
                {
                    this.RegisterOAuthDependencies();
                }

                // Register claims related dependencies
                this.RegisterClaimsDependencies(container);
            }
        }

        /*
         * Register dependencies specific to logging
         */
        private void RegisterBaseDependencies()
        {
            // The log entry is scoped to the current request and created via this factory method
            this.services.AddSingleton(this.loggingConfiguration);
            this.services.AddScoped<ILogEntry>(
                ctx =>
                {
                    return this.loggerFactory.CreateLogEntry();
                });

            // Register HTTP related objects
            this.services.AddSingleton(this.httpProxy);
            this.services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        }

        /*
         * Register OAuth dependencies
         */
        private void RegisterOAuthDependencies()
        {
            this.services.AddSingleton(this.oauthConfiguration);

            // Register the authorizer as a per request dependency
            if (this.oauthConfiguration.Provider == "cognito")
            {
                this.services.AddScoped<IAuthorizer, ClaimsCachingAuthorizer>();
            }
            else
            {
                this.services.AddScoped<IAuthorizer, StandardAuthorizer>();
            }

            // The authenticator is a per request dependency but uses cached JWKS keys
            this.services.AddSingleton(new JsonWebKeyResolver(this.oauthConfiguration, this.httpProxy));
            this.services.AddScoped<OAuthAuthenticator>();
        }

        /*
         * Register Claims related dependencies
         */
        private void RegisterClaimsDependencies(ServiceProvider container)
        {
            // Register the singleton cache if using claims caching
            if (this.oauthConfiguration.Provider == "cognito")
            {
                this.services.AddDistributedMemoryCache();
                var cache = container.GetService<IDistributedCache>();

                var claimsCache = new ClaimsCache(
                    cache,
                    this.oauthConfiguration.ClaimsCacheTimeToLiveMinutes,
                    this.customClaimsProvider,
                    container);
                this.services.AddSingleton(claimsCache);
            }

            // Register an object to manage custom claims
            this.services.AddSingleton(this.customClaimsProvider);

            // Claims are injected into this holder at runtime
            this.services.AddScoped<ClaimsHolder>();

            // The underlying claims objects can then be retrieved via these factory methods
            this.services.AddScoped(ctx => ctx.GetService<ClaimsHolder>().Value.Base);
            this.services.AddScoped(ctx => ctx.GetService<ClaimsHolder>().Value.UserInfo);
            this.services.AddScoped(ctx => ctx.GetService<ClaimsHolder>().Value.Custom);
        }
    }
}
