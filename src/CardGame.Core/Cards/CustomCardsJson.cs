using System.Text.Json;

namespace CardGame.Core.Cards;

/// <summary>作成したカードの一覧を、保存用の JSON に変換する。形式は cards.json の "cards" と同じ。</summary>
public static class CustomCardsJson
{
    public static string Serialize(IReadOnlyList<CardData> cards) =>
        JsonSerializer.Serialize(cards, CardJsonContext.Default.IReadOnlyListCardData);

    /// <summary>
    /// 保存されていた JSON をカードの一覧に戻す。壊れている・形式が違う場合は null
    /// (保存領域はブラウザ側で消されたり書き換えられたりし得るため、例外ではなく通常の失敗として扱う)。
    /// </summary>
    public static IReadOnlyList<CardData>? TryParse(string json)
    {
        try
        {
            var cards = JsonSerializer.Deserialize(json, CardJsonContext.Default.IReadOnlyListCardData);
            return cards is not null && cards.All(c => c is not null) ? cards : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
