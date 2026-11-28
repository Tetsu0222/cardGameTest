using System.Text.Json.Serialization;
using CardGame.Core.Decks;

namespace CardGame.Core.Cards;

/// <summary>カードデータ(cards.json)のファイル全体。</summary>
internal sealed record CardCatalogFile(IReadOnlyList<CardData> Cards, DeckList DefaultDeck);

/// <summary>
/// カード・デッキの JSON 変換の設定。Blazor WebAssembly の配布時のトリミングでも動くよう、ソース生成を使う。
/// プロパティ名はキャメルケース、列挙値は C# の名前のまま(例: <c>"kind": "NormalMonster"</c>)。
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = true)]
[JsonSerializable(typeof(CardCatalogFile))]
[JsonSerializable(typeof(DeckList))]
internal sealed partial class CardJsonContext : JsonSerializerContext;
