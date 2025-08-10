using Wpf.Ui.Abstractions;

namespace AppSwitcher.UI.Pages;

public class PageProviderService(IServiceProvider serviceProvider) : INavigationViewPageProvider
{
    private readonly Dictionary<Type, object> _pageCache = new();

    public object GetPage(Type pageType)
    {
        ArgumentNullException.ThrowIfNull(pageType);

        if (_pageCache.TryGetValue(pageType, out var cachedPage))
        {
            return cachedPage;
        }

        // Use the service provider to create an instance of the page
        var page = serviceProvider.GetService(pageType)
                   ?? throw new InvalidOperationException($"Page of type {pageType.Name} could not be created.");

        _pageCache.Add(pageType, page);

        return page;
    }
}