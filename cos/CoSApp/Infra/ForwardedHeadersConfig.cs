using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

namespace CosApp.Infra
{
    public static class ForwardedHeadersConfig
    {
        public static IServiceCollection AddConfiguredForwardedHeaders(
            this IServiceCollection services)
        {
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor |
                    ForwardedHeaders.XForwardedProto;

                ConfigureKnownProxies(options);
                ConfigureKnownNetworks(options);
                ConfigureForwardLimit(options);

                options.RequireHeaderSymmetry = false;
            });

            return services;
        }

        // --------------------------------------------------
        // KnownProxies
        // --------------------------------------------------
        private static void ConfigureKnownProxies(ForwardedHeadersOptions options)
        {
            var proxiesEnv = Environment.GetEnvironmentVariable("TRUSTED_REVERSE_PROXIES");

            if (!string.IsNullOrWhiteSpace(proxiesEnv))
            {
                foreach (var entry in proxiesEnv.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    var value = entry.Trim();
                    if (IPAddress.TryParse(value, out var ip))
                    {
                        options.KnownProxies.Add(ip);
                    }
                }
            }
            else
            {
                // Sane defaults (localhost reverse proxy)
                options.KnownProxies.Add(IPAddress.Parse("127.0.0.1"));
                options.KnownProxies.Add(IPAddress.Parse("::1"));
            }
        }

        // --------------------------------------------------
        // KnownNetworks
        // --------------------------------------------------
        private static void ConfigureKnownNetworks(ForwardedHeadersOptions options)
        {
            var networksEnv = Environment.GetEnvironmentVariable("TRUSTED_REVERSE_NETWORKS");

            if (string.IsNullOrWhiteSpace(networksEnv))
                return;

            foreach (var entry in networksEnv.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var value = entry.Trim();
                var parts = value.Split('/', StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length == 2 &&
                    IPAddress.TryParse(parts[0], out var networkIp) &&
                    int.TryParse(parts[1], out var prefix))
                {
                    options.KnownNetworks.Add(new IPNetwork(networkIp, prefix));
                }
            }
        }

        // --------------------------------------------------
        // ForwardLimit
        // --------------------------------------------------
        private static void ConfigureForwardLimit(ForwardedHeadersOptions options)
        {
            var limitEnv = Environment.GetEnvironmentVariable("FORWARDED_HEADERS_LIMIT");

            if (string.IsNullOrWhiteSpace(limitEnv) ||
                limitEnv.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                options.ForwardLimit = null; // unlimited hops
                return;
            }

            if (int.TryParse(limitEnv, out var limit) && limit > 0)
            {
                options.ForwardLimit = limit;
            }
            else
            {
                options.ForwardLimit = 2; // safe fallback
            }
        }
    }
}

