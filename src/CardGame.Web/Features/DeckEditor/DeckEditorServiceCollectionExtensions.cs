using Microsoft.Extensions.DependencyInjection;

namespace CardGame.Web.Features.DeckEditor;

public static class DeckEditorServiceCollectionExtensions
{
    public static IServiceCollection AddDeckEditorServices(this IServiceCollection services)
    {
        services.AddScoped<DeckEditorViewModel>();
        return services;
    }
}
