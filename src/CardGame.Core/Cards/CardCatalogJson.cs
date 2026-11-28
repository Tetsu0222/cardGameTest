using System.Text.Json;

namespace CardGame.Core.Cards;

/// <summary>
/// カードデータ(cards.json)を読み込む。ファイルの取得は呼び出し側(Web)が行い、ここは文字列の変換と検証だけを行う。
/// </summary>
/// <remarks>
/// 形式の例:
/// <code>
/// {
///   "cards": [
///     { "id": 1, "name": "ブレイズ・ウルフ", "kind": "NormalMonster", "level": 4, "attack": 1800, "defense": 1000 },
///     { "id": 102, "name": "火炎弾", "kind": "Spell", "effect": { "actions": [ { "type": "DealDamage", "amount": 600 } ] } }
///   ],
///   "defaultDeck": { "name": "初期デッキ", "entries": [ { "cardId": 1, "count": 3 } ] }
/// }
/// </code>
/// </remarks>
public static class CardCatalogJson
{
    /// <exception cref="InvalidDataException">JSON の形式が正しくない、またはカードの定義が成り立っていない場合。</exception>
    public static CardCatalog Parse(string json)
    {
        CardCatalogFile? file;
        try
        {
            file = JsonSerializer.Deserialize(json, CardJsonContext.Default.CardCatalogFile);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Card data is not valid JSON: {ex.Message}", ex);
        }

        if (file?.Cards is null || file.DefaultDeck is null)
        {
            throw new InvalidDataException("Card data must have \"cards\" and \"defaultDeck\".");
        }

        var invalid = file.Cards.FirstOrDefault(card => !ValidateCardData.Execute(card));
        if (invalid is not null)
        {
            throw new InvalidDataException($"Invalid card definition: id={invalid.Id} name={invalid.Name}");
        }

        try
        {
            return new CardCatalog(file.Cards, file.DefaultDeck);
        }
        catch (ArgumentException ex)
        {
            throw new InvalidDataException(ex.Message, ex);
        }
    }

    /// <summary>カードデータを JSON にする(将来のカード作成機能での保存用)。</summary>
    public static string Serialize(CardCatalog catalog) =>
        JsonSerializer.Serialize(new CardCatalogFile(catalog.Cards, catalog.DefaultDeck), CardJsonContext.Default.CardCatalogFile);
}
