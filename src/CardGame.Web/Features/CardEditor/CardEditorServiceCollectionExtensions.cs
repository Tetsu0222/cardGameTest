using Microsoft.Extensions.DependencyInjection;

namespace CardGame.Web.Features.CardEditor;

public static class CardEditorServiceCollectionExtensions
{
    public static IServiceCollection AddCardEditorServices(this IServiceCollection services)
    {
        services.AddScoped<CardEditorViewModel>();
        return services;
    }
}
