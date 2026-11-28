using CardGame.Core.Cards;
using CardGame.Core.Decks;

namespace CardGame.Tests.TestSupport;

/// <summary>配布するカードデータ(src/CardGame.Web/wwwroot/data/cards.json)をテストで使う。</summary>
internal static class TestCatalog
{
    private static readonly Lazy<CardCatalog> Catalog = new(() =>
        CardCatalogJson.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "cards.json"))));

    public static CardCatalog Load() => Catalog.Value;

    /// <summary>初期デッキのカードの並び(シャッフル前)。</summary>
    public static IReadOnlyList<CardData> DefaultDeckCards() => BuildDeck.Execute(Load().DefaultDeck, Load());
}
