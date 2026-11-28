using CardGame.Core.Cards;
using CardGame.Core.Decks;
using CardGame.Core.Duels;
using CardGame.Web.Services;
using Microsoft.Extensions.Options;

namespace CardGame.Web.Shared;

/// <summary>
/// 画面をまたいで使うゲームのデータ(カードデータと、プレイヤーのデッキ)。
/// アプリの起動時に <see cref="InitializeAsync"/> で読み込んでから画面を表示する。
/// </summary>
public sealed class GameData(CardCatalogClient catalogClient, IDeckRepository deckRepository, IOptions<GameRulesOptions> rules)
{
    private readonly GameRulesOptions _rules = rules.Value;
    private CardCatalog? _catalog;

    /// <summary>ゲームで使えるすべてのカード。</summary>
    public CardCatalog Catalog => _catalog ?? throw new InvalidOperationException("GameData has not been initialized.");

    /// <summary>プレイヤーが対戦で使うデッキ。保存していなければ初期デッキ。</summary>
    public DeckList PlayerDeck { get; private set; } = new(string.Empty, []);

    /// <summary>
    /// カードデータと保存してあるデッキを読み込む。保存してあるデッキがルールに合っていない場合
    /// (カードデータの更新でカードがなくなったなど)は、初期デッキを使う。
    /// </summary>
    public async Task InitializeAsync()
    {
        _catalog = await catalogClient.LoadAsync();

        var saved = await deckRepository.LoadAsync();
        PlayerDeck = saved is not null && IsValid(saved) ? saved : _catalog.DefaultDeck;
    }

    /// <summary>プレイヤーのデッキを保存し、次の対戦から使うようにする。</summary>
    /// <returns>保存できたか。ルールに合っていない、または保存領域が使えない場合は false。</returns>
    public async Task<bool> SavePlayerDeckAsync(DeckList deck)
    {
        if (!IsValid(deck)) return false;
        if (!await deckRepository.SaveAsync(deck)) return false;

        PlayerDeck = deck;
        return true;
    }

    private bool IsValid(DeckList deck) => ValidateDeck.Execute(deck, Catalog, _rules).Count == 0;
}
