using CardGame.Core.Cards;
using CardGame.Core.Decks;
using CardGame.Core.Duels;
using CardGame.Web.Shared;
using Microsoft.Extensions.Options;

namespace CardGame.Web.Features.DeckEditor;

/// <summary>カード一覧の絞り込み。</summary>
public enum CardKindFilter
{
    All,
    Monster,
    Spell,
    Trap,

    /// <summary>プレイヤーが作ったカード。</summary>
    Custom,
}

/// <summary>
/// デッキ編集画面の状態と操作。編集中のデッキは保存するまで対戦には使われない。
/// デッキのルール判定は Core の <see cref="ValidateDeck"/> に任せる。
/// </summary>
public sealed class DeckEditorViewModel
{
    private const string DefaultDeckName = "マイデッキ";

    private readonly GameData _gameData;
    private readonly GameRulesOptions _rules;
    private readonly Dictionary<int, int> _counts = [];
    private string _deckName = DefaultDeckName;

    public DeckEditorViewModel(GameData gameData, IOptions<GameRulesOptions> rules)
    {
        _gameData = gameData;
        _rules = rules.Value;
        LoadFrom(gameData.PlayerDeck);
    }

    public CardKindFilter Filter { get; set; } = CardKindFilter.All;

    public string DeckName
    {
        get => _deckName;
        set
        {
            if (_deckName == value) return;
            _deckName = value;
            MarkDirty();
        }
    }

    /// <summary>保存していない変更があるか。</summary>
    public bool IsDirty { get; private set; }

    public bool IsSaving { get; private set; }

    /// <summary>保存の結果などのお知らせ。</summary>
    public string? StatusMessage { get; private set; }

    public bool IsStatusError { get; private set; }

    public int MinDeckSize => _rules.MinDeckSize;
    public int MaxDeckSize => _rules.MaxDeckSize;
    public int MaxCopiesPerCard => _rules.MaxCopiesPerCard;

    /// <summary>絞り込み後のカード一覧(カードデータの定義順)。</summary>
    public IReadOnlyList<CardData> VisibleCards => [.. _gameData.Catalog.Cards.Where(Matches)];

    public int TotalCount => _counts.Values.Sum();

    public int CountOf(CardData card) => _counts.GetValueOrDefault(card.Id);

    public int CountOfKind(CardKindFilter kind) =>
        _counts.Where(pair => _gameData.Catalog.Find(pair.Key) is { } card && IsKind(card, kind)).Sum(pair => pair.Value);

    /// <summary>デッキに入っているカードと枚数(カードデータの定義順)。</summary>
    public IReadOnlyList<DeckEntryView> Contents =>
    [
        .. _gameData.Catalog.Cards
            .Where(card => CountOf(card) > 0)
            .Select(card => new DeckEntryView(card, CountOf(card))),
    ];

    /// <summary>編集中のデッキがルールに合っていない理由。合っていれば空。</summary>
    public IReadOnlyList<string> ErrorMessages =>
    [
        .. ValidateDeck.Execute(CurrentDeck, _gameData.Catalog, _rules)
            .Select(error => DeckErrorMessages.ToMessage(error, _gameData.Catalog, _rules, TotalCount)),
    ];

    public bool CanSave => IsDirty && !IsSaving && ErrorMessages.Count == 0;

    public bool CanAdd(CardData card) => CountOf(card) < _rules.MaxCopiesPerCard && TotalCount < _rules.MaxDeckSize;

    public bool CanRemove(CardData card) => CountOf(card) > 0;

    public void Add(CardData card)
    {
        if (!CanAdd(card)) return;
        _counts[card.Id] = CountOf(card) + 1;
        MarkDirty();
    }

    public void Remove(CardData card)
    {
        if (!CanRemove(card)) return;
        var count = CountOf(card) - 1;
        if (count == 0) _counts.Remove(card.Id);
        else _counts[card.Id] = count;
        MarkDirty();
    }

    public async Task SaveAsync()
    {
        if (!CanSave) return;

        IsSaving = true;
        try
        {
            var saved = await _gameData.SavePlayerDeckAsync(CurrentDeck);
            IsDirty = !saved;
            SetStatus(saved
                ? "保存しました。次の対戦から使われます。"
                : "保存できませんでした。ブラウザの設定でサイトのデータの保存が許可されているか確認してください。",
                isError: !saved);
        }
        finally
        {
            IsSaving = false;
        }
    }

    /// <summary>編集をやめ、保存してあるデッキ(なければ初期デッキ)に戻す。</summary>
    public void RevertToSaved()
    {
        LoadFrom(_gameData.PlayerDeck);
        SetStatus("保存してある内容に戻しました。", isError: false);
    }

    /// <summary>初期デッキの構成を読み込む(保存するまでは対戦に使われない)。</summary>
    public void LoadDefaultDeck()
    {
        LoadFrom(_gameData.Catalog.DefaultDeck, name: _deckName);
        MarkDirty();
    }

    public void Clear()
    {
        _counts.Clear();
        MarkDirty();
    }

    private DeckList CurrentDeck =>
        new(
            string.IsNullOrWhiteSpace(_deckName) ? DefaultDeckName : _deckName.Trim(),
            [.. Contents.Select(entry => new DeckEntry(entry.Card.Id, entry.Count))]);

    private void LoadFrom(DeckList deck, string? name = null)
    {
        _counts.Clear();
        foreach (var entry in deck.Entries.Where(e => e.Count > 0))
        {
            _counts[entry.CardId] = _counts.GetValueOrDefault(entry.CardId) + entry.Count;
        }

        _deckName = name ?? deck.Name;
        IsDirty = false;
    }

    private void MarkDirty()
    {
        IsDirty = true;
        StatusMessage = null;
    }

    private void SetStatus(string message, bool isError)
    {
        StatusMessage = message;
        IsStatusError = isError;
    }

    private bool Matches(CardData card) => IsKind(card, Filter);

    private static bool IsKind(CardData card, CardKindFilter kind) => kind switch
    {
        CardKindFilter.Monster => card.Kind is CardKind.NormalMonster or CardKind.EffectMonster,
        CardKindFilter.Spell => card.Kind == CardKind.Spell,
        CardKindFilter.Trap => card.Kind == CardKind.Trap,
        CardKindFilter.Custom => ValidateCustomCard.IsCustomCardId(card.Id),
        _ => true,
    };
}
