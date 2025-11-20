using Microsoft.Extensions.DependencyInjection;

namespace KaxamlPlugins.DependencyInjection.Registration;

/// <summary>
/// Registers a Service Type with a DI Service.
/// </summary>
/// <typeparam name="TService">Type of source to be registered</typeparam>
public sealed record TypeDiRegistration<TService> : IDiRegistration
    where TService : class
{
    public void RegisterSingleton(IServiceCollection services)
        => services.AddSingleton<TService>();
}