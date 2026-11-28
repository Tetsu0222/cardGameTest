using CardGame.Core.Cards;
using CardGame.Core.Effects;
using CardGame.Web.Shared;
using Microsoft.Extensions.Options;

namespace CardGame.Web.Features.CardEditor;

/// <summary>
/// カード作成画面の状態と操作。数値は <see cref="CardCreationOptions"/> の範囲・刻みから選ぶ。
/// カードが作成のルールに合っているかの判定は Core の <see cref="ValidateCustomCard"/> に任せる。
/// </summary>
public sealed class CardEditorViewModel
{
    private readonly GameData _gameData;

    public CardEditorViewModel(GameData gameData, IOptions<CardCreationOptions> options)
    {
        _gameData = gameData;
        Options = options.Value;
        Draft = new CardDraft();
    }

    public CardCreationOptions Options { get; }

    /// <summary>作ったカード(作った順)。</summary>
    public IReadOnlyList<CardData> CustomCards => _gameData.CustomCards;

    /// <summary>編集中のカード。</summary>
    public CardDraft Draft { get; private set; }

    /// <summary>既存のカードを編集中か(新しく作っている途中なら false)。</summary>
    public bool IsEditingExisting => Draft.Id is not null;

    public bool IsSaving { get; private set; }

    public string? StatusMessage { get; private set; }

    public bool IsStatusError { get; private set; }

    /// <summary>編集中のカードを、保存したときの見た目で表示するためのカード定義。</summary>
    public CardData Preview => Draft.ToCardData(Draft.Id ?? IssueCustomCardId.Execute(_gameData.Catalog.Cards));

    public IReadOnlyList<string> ErrorMessages =>
    [
        .. ValidateCustomCard.Execute(Preview, Options).Select(error => CustomCardErrorMessages.ToMessage(error, Options)),
    ];

    public bool CanSave => !IsSaving && ErrorMessages.Count == 0;

    // ---- 選択肢 ----

    public IReadOnlyList<int> Levels => [.. Enumerable.Range(Options.MinLevel, Options.MaxLevel - Options.MinLevel + 1)];

    public IReadOnlyList<int> Stats => EffectPartChoices.Steps(0, Options.MaxStat, Options.StatStep);

    public IReadOnlyList<EffectPartType> AvailableParts => EffectPartChoices.Available(Draft.Kind, Draft.Trigger);

    public bool CanAddPart => Draft.Parts.Count < Options.MaxEffectActions;

    // ---- 作る・編集する ----

    public void StartNew()
    {
        Draft = new CardDraft();
        ClearStatus();
    }

    public void StartEdit(CardData card)
    {
        Draft = CardDraft.FromCard(card);
        ClearStatus();
    }

    /// <summary>カードの種類を変える。新しい種類で選べない効果部品は外す。</summary>
    public void ChangeKind(CardKind kind)
    {
        Draft.Kind = kind;
        RemoveUnavailableParts();
    }

    /// <summary>罠の発動タイミングを変える。新しいタイミングで選べない効果部品は外す。</summary>
    public void ChangeTrigger(TrapTrigger trigger)
    {
        Draft.Trigger = trigger;
        RemoveUnavailableParts();
    }

    public void AddPart(EffectPartType type)
    {
        if (!CanAddPart || !AvailableParts.Contains(type)) return;
        Draft.Parts.Add(new EffectPartDraft { Type = type, Value = EffectPartChoices.DefaultValue(type, Options) });
    }

    public void RemovePart(EffectPartDraft part) => Draft.Parts.Remove(part);

    /// <summary>効果部品を1つ前に動かす(効果は上から順に処理される)。</summary>
    public void MovePartUp(EffectPartDraft part)
    {
        var index = Draft.Parts.IndexOf(part);
        if (index <= 0) return;
        (Draft.Parts[index - 1], Draft.Parts[index]) = (Draft.Parts[index], Draft.Parts[index - 1]);
    }

    public async Task SaveAsync()
    {
        if (!CanSave) return;

        IsSaving = true;
        try
        {
            var card = Preview;
            var result = await _gameData.SaveCustomCardAsync(card);
            if (result == CustomCardChangeResult.Succeeded)
            {
                // 続けて同じカードを直せるよう、保存したカードの編集に切り替える
                Draft = CardDraft.FromCard(card);
            }

            SetStatus(result, succeededMessage: $"「{card.Name}」を保存しました。デッキ編集で使えます。");
        }
        finally
        {
            IsSaving = false;
        }
    }

    public bool IsUsedInDeck(CardData card) => _gameData.IsUsedInPlayerDeck(card.Id);

    public async Task DeleteAsync(CardData card)
    {
        var result = await _gameData.DeleteCustomCardAsync(card.Id);
        if (result == CustomCardChangeResult.Succeeded && Draft.Id == card.Id)
        {
            Draft = new CardDraft();
        }

        SetStatus(result, succeededMessage: $"「{card.Name}」を削除しました。");
    }

    private void RemoveUnavailableParts() => Draft.Parts.RemoveAll(p => !AvailableParts.Contains(p.Type));

    private void SetStatus(CustomCardChangeResult result, string succeededMessage)
    {
        StatusMessage = result switch
        {
            CustomCardChangeResult.Succeeded => succeededMessage,
            CustomCardChangeResult.UsedInDeck => "保存したデッキに入っているカードは削除できません。先にデッキから外して保存してください。",
            CustomCardChangeResult.StorageFailed => "保存できませんでした。ブラウザの設定でサイトのデータの保存が許可されているか確認してください。",
            _ => "カードの内容がルールに合っていないため保存できません。",
        };
        IsStatusError = result != CustomCardChangeResult.Succeeded;
    }

    private void ClearStatus()
    {
        StatusMessage = null;
        IsStatusError = false;
    }
}
