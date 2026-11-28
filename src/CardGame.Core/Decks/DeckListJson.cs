using System.Text.Json;
using CardGame.Core.Cards;

namespace CardGame.Core.Decks;

/// <summary>デッキ構成を保存用の JSON に変換する。</summary>
public static class DeckListJson
{
    public static string Serialize(DeckList deck) =>
        JsonSerializer.Serialize(deck, CardJsonContext.Default.DeckList);

    /// <summary>
    /// 保存されていた JSON をデッキ構成に戻す。壊れている・形式が違う場合は null
    /// (保存領域はブラウザ側で消されたり書き換えられたりし得るため、例外ではなく通常の失敗として扱う)。
    /// </summary>
    public static DeckList? TryParse(string json)
    {
        try
        {
            var deck = JsonSerializer.Deserialize(json, CardJsonContext.Default.DeckList);
            return deck is { Name: not null, Entries: not null } ? deck : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
