using System;
using Microsoft.Extensions.DependencyInjection;

namespace BellwoodGlobal.Mobile
{
    public static class ServiceHelper
    {
        private static IServiceProvider? _services;

        public static void Initialize(IServiceProvider services) => _services = services;

        public static T GetService<T>() where T : notnull =>
            _services.GetRequiredService<T>()
            ?? throw new InvalidOperationException("Service provider not initialized.");
    }
}
