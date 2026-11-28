using CardGame.Core.Decks;
using CardGame.Web.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CardGame.Web.Shared;

public static class GameDataServiceCollectionExtensions
{
    /// <summary>
    /// カードデータとデッキの読み書きを登録する。起動時に読み込んだデータを全画面で共有するため、すべて Singleton にする。
    /// </summary>
    public static IServiceCollection AddGameData(this IServiceCollection services, Uri baseAddress)
    {
        services.AddSingleton(new HttpClient { BaseAddress = baseAddress });
        services.AddSingleton<CardCatalogClient>();
        services.AddSingleton<IDeckRepository, LocalStorageDeckRepository>();
        services.AddSingleton<GameData>();
        return services;
    }
}
