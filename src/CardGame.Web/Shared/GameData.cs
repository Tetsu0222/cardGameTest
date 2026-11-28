using CardGame.Core.Cards;
using CardGame.Core.Decks;
using CardGame.Core.Duels;
using CardGame.Web.Services;
using Microsoft.Extensions.Options;

namespace CardGame.Web.Shared;

/// <summary>
/// 画面をまたいで使うゲームのデータ(カードデータ・プレイヤーが作ったカード・プレイヤーのデッキ)。
/// アプリの起動時に <see cref="InitializeAsync"/> で読み込んでから画面を表示する。
/// </summary>
public sealed class GameData(
    CardCatalogClient catalogClient,
    IDeckRepository deckRepository,
    ICustomCardRepository customCardRepository,
    IOptions<GameRulesOptions> rules,
    IOptions<CardCreationOptions> creationOptions)
{
    private readonly GameRulesOptions _rules = rules.Value;
    private readonly CardCreationOptions _creationOptions = creationOptions.Value;
    private CardCatalog? _builtInCatalog;
    private CardCatalog? _catalog;
    private IReadOnlyList<CardData> _customCards = [];

    /// <summary>ゲームで使えるすべてのカード(組み込みのカード + プレイヤーが作ったカード)。</summary>
    public CardCatalog Catalog => _catalog ?? throw new InvalidOperationException("GameData has not been initialized.");

    /// <summary>プレイヤーが作ったカード(作った順)。</summary>
    public IReadOnlyList<CardData> CustomCards => _customCards;

    /// <summary>プレイヤーが対戦で使うデッキ。保存していなければ初期デッキ。</summary>
    public DeckList PlayerDeck { get; private set; } = new(string.Empty, []);

    /// <summary>
    /// カードデータ・作ったカード・保存してあるデッキを読み込む。
    /// 作ったカードのうちルールに合っていないもの、デッキのうちルールに合っていないものは使わない
    /// (保存領域が書き換えられた、カードデータが更新されたなど)。
    /// </summary>
    public async Task InitializeAsync()
    {
        _builtInCatalog = await catalogClient.LoadAsync();

        var savedCards = await customCardRepository.LoadAsync();
        _customCards = [.. savedCards
            .Where(card => ValidateCustomCard.Execute(card, _creationOptions).Count == 0)
            .DistinctBy(card => card.Id)];
        _catalog = _builtInCatalog.WithAdditionalCards(_customCards);

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

    /// <summary>作ったカードを保存する。同じ ID のカードがあれば置き換える(編集)。</summary>
    public async Task<CustomCardChangeResult> SaveCustomCardAsync(CardData card)
    {
        if (ValidateCustomCard.Execute(card, _creationOptions).Count > 0) return CustomCardChangeResult.Invalid;

        var index = _customCards.ToList().FindIndex(c => c.Id == card.Id);
        IReadOnlyList<CardData> updated = index < 0
            ? [.. _customCards, card]
            : [.. _customCards.Select(c => c.Id == card.Id ? card : c)];

        return await ReplaceCustomCardsAsync(updated);
    }

    /// <summary>作ったカードを削除する。保存したデッキに入っているカードは削除できない。</summary>
    public async Task<CustomCardChangeResult> DeleteCustomCardAsync(int cardId)
    {
        if (IsUsedInPlayerDeck(cardId)) return CustomCardChangeResult.UsedInDeck;

        return await ReplaceCustomCardsAsync([.. _customCards.Where(c => c.Id != cardId)]);
    }

    /// <summary>保存したデッキに入っているか。</summary>
    public bool IsUsedInPlayerDeck(int cardId) => PlayerDeck.CountOf(cardId) > 0;

    private async Task<CustomCardChangeResult> ReplaceCustomCardsAsync(IReadOnlyList<CardData> cards)
    {
        if (!await customCardRepository.SaveAsync(cards)) return CustomCardChangeResult.StorageFailed;

        _customCards = cards;
        _catalog = (_builtInCatalog ?? throw new InvalidOperationException("GameData has not been initialized."))
            .WithAdditionalCards(cards);
        return CustomCardChangeResult.Succeeded;
    }

    private bool IsValid(DeckList deck) => ValidateDeck.Execute(deck, Catalog, _rules).Count == 0;
}
