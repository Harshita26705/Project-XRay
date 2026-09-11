namespace XRay.Api.Services.Repositories;

public class RepositoryProviderFactory
{
    private readonly IReadOnlyDictionary<string, IRepositoryProvider> _providers;

    public RepositoryProviderFactory(IEnumerable<IRepositoryProvider> providers)
    {
        _providers = providers.ToDictionary(p => p.ProviderCode);
    }

    public IRepositoryProvider Resolve(string providerCode) =>
        _providers.TryGetValue(providerCode, out var provider)
            ? provider
            : throw new InvalidOperationException($"No repository provider registered for '{providerCode}'.");
}
