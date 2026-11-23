using CardGame.AI.Strategies;
using CardGame.Core.Duels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CardGame.Web.Features.Duels;

public static class DuelServiceCollectionExtensions
{
    public static IServiceCollection AddDuelServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<GameRulesOptions>(configuration.GetSection("GameRules"));
        services.AddSingleton(new Random());
        services.AddSingleton<SimpleCpuStrategy>();
        services.AddScoped<DuelViewModel>();
        return services;
    }
}
