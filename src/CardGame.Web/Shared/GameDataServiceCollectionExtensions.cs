using CardGame.Core.Cards;
using CardGame.Core.Decks;
using CardGame.Web.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CardGame.Web.Shared;

public static class GameDataServiceCollectionExtensions
{
    /// <summary>
    /// カードデータ・作ったカード・デッキの読み書きを登録する。起動時に読み込んだデータを全画面で共有するため、すべて Singleton にする。
    /// </summary>
    public static IServiceCollection AddGameData(this IServiceCollection services, IConfiguration configuration, Uri baseAddress)
    {
        services.Configure<CardCreationOptions>(configuration.GetSection("CardCreation"));
        services.AddSingleton(new HttpClient { BaseAddress = baseAddress });
        services.AddSingleton<CardCatalogClient>();
        services.AddSingleton<IDeckRepository, LocalStorageDeckRepository>();
        services.AddSingleton<ICustomCardRepository, LocalStorageCustomCardRepository>();
        services.AddSingleton<GameData>();
        return services;
    }
}
