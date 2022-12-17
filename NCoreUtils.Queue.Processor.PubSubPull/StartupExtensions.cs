using System;
using Microsoft.Extensions.Configuration;

namespace NCoreUtils.Queue;

internal static class StartupExtensions
{
    public static string GetRequiredValue(this IConfiguration configuration, string key)
    {
        var value = configuration[key];
        if (string.IsNullOrEmpty(value))
        {
            var path = configuration is IConfigurationSection section ? $"{section.Path}:{key}" : key;
            throw new InvalidOperationException($"No required value found at {path}");
        }
        return value;
    }
}