using CardGame.Core.Cards;

namespace CardGame.Web.Services;

/// <summary>カードデータ(wwwroot/data/cards.json)を取得する。</summary>
public sealed class CardCatalogClient(HttpClient http)
{
    private const string CatalogPath = "data/cards.json";

    /// <exception cref="HttpRequestException">ファイルを取得できなかった場合。</exception>
    /// <exception cref="InvalidDataException">カードデータの内容が正しくない場合。</exception>
    public async Task<CardCatalog> LoadAsync()
    {
        var json = await http.GetStringAsync(CatalogPath);
        return CardCatalogJson.Parse(json);
    }
}
