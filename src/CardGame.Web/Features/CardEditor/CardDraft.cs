using CardGame.Core.Cards;
using CardGame.Core.Effects;

namespace CardGame.Web.Features.CardEditor;

/// <summary>
/// カード作成画面で編集中のカード(フォームの入力値)。保存するときに <see cref="ToCardData"/> で Core のカード定義に変える。
/// </summary>
public sealed class CardDraft
{
    /// <summary>編集中の既存カードの ID。新しく作るカードなら null。</summary>
    public int? Id { get; init; }

    public string Name { get; set; } = string.Empty;
    public CardKind Kind { get; set; } = CardKind.NormalMonster;

    public int Level { get; set; } = 4;
    public int Attack { get; set; } = 1000;
    public int Defense { get; set; } = 1000;

    /// <summary>罠の発動タイミング。</summary>
    public TrapTrigger Trigger { get; set; } = TrapTrigger.OpponentAttackDeclared;

    /// <summary>効果モンスターの効果の発動タイミング。</summary>
    public MonsterEffectTrigger MonsterTrigger { get; set; } = MonsterEffectTrigger.Flipped;

    /// <summary>対象を取る部品があるときの、対象にできる側。</summary>
    public MonsterSide TargetSide { get; set; } = MonsterSide.Opponent;

    public List<EffectPartDraft> Parts { get; } = [];

    /// <summary>レベル・攻撃力・守備力を持つか(通常モンスター・効果モンスター)。</summary>
    public bool IsMonster => Kind is CardKind.NormalMonster or CardKind.EffectMonster;

    /// <summary>効果を持つか(効果モンスター・魔法・罠)。</summary>
    public bool HasEffect => Kind != CardKind.NormalMonster;

    public bool HasTargetPart => Parts.Any(p => p.Type == EffectPartType.DestroyTargetMonster);

    public CardData ToCardData(int id)
    {
        if (!HasEffect)
        {
            return new CardData(id, Name.Trim(), Kind, Level, Attack, Defense);
        }

        var effect = new CardEffect(
            [.. Parts.Select(p => p.ToAction())],
            HasTargetPart ? TargetSide : null,
            Kind == CardKind.Trap ? Trigger : null,
            Kind == CardKind.EffectMonster ? MonsterTrigger : null);
        return IsMonster
            ? new CardData(id, Name.Trim(), Kind, Level, Attack, Defense, effect)
            : new CardData(id, Name.Trim(), Kind, Effect: effect);
    }

    public static CardDraft FromCard(CardData card)
    {
        var draft = new CardDraft
        {
            Id = card.Id,
            Name = card.Name,
            Kind = card.Kind,
            Level = card.Level,
            Attack = card.Attack,
            Defense = card.Defense,
            Trigger = card.Effect?.Trigger ?? TrapTrigger.OpponentAttackDeclared,
            TargetSide = card.Effect?.TargetSide ?? MonsterSide.Opponent,
            MonsterTrigger = card.Effect?.MonsterTrigger ?? MonsterEffectTrigger.Flipped,
        };
        draft.Parts.AddRange(card.Effect?.Actions.Select(EffectPartDraft.FromAction) ?? []);
        return draft;
    }
}
