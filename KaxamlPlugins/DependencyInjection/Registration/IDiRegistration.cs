using Microsoft.Extensions.DependencyInjection;

namespace KaxamlPlugins.DependencyInjection.Registration;

/// <summary>
/// Registers with a Dependency Injection Service.
/// </summary>
public interface IDiRegistration
{
    /// <summary>
    /// Registers the source with the DI Service as a Singleton.
    /// </summary>
    /// <param name="services">DI Service to register with.</param>
    void RegisterSingleton(IServiceCollection services);
}