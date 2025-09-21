using Microsoft.Extensions.DependencyInjection;

namespace BellwoodGlobal.Mobile
{
    public static class ServiceHelper
    {
        // If you already have these, keep your versions and don't duplicate them.
        public static IServiceProvider? Services { get; private set; }
        public static void Initialize(IServiceProvider services) => Services = services;

        // Add these helper methods:
        public static T GetRequiredService<T>() where T : notnull
        {
            if (Services is null)
                throw new InvalidOperationException(
                    "ServiceHelper not initialized. Call ServiceHelper.Initialize(app.Services) in MauiProgram.");
            return Services.GetRequiredService<T>();
        }

        public static object GetRequiredService(Type type)
        {
            if (Services is null)
                throw new InvalidOperationException(
                    "ServiceHelper not initialized. Call ServiceHelper.Initialize(app.Services) in MauiProgram.");
            return Services.GetRequiredService(type);
        }

        public static T? GetService<T>() where T : class => Services?.GetService<T>();
    }
}
