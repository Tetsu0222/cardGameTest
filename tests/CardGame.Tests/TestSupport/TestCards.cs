using CardGame.Core.Cards;
using CardGame.Core.Effects;

namespace CardGame.Tests.TestSupport;

/// <summary>テスト用のカード定義ファクトリ。</summary>
internal static class TestCards
{
    public static CardData Monster(int attack = 1000, int defense = 1000, int level = 4) =>
        new(1, $"Monster{attack}/{defense}", CardKind.NormalMonster, level, attack, defense);

    /// <summary>効果モンスター(対象を取らない効果)。</summary>
    public static CardData EffectMonster(MonsterEffectTrigger trigger, int attack = 500, int defense = 500, params EffectAction[] actions) =>
        new(7, $"EffectMonster{trigger}", CardKind.EffectMonster, 3, attack, defense, new CardEffect(actions, MonsterTrigger: trigger));

    /// <summary>リバース: モンスター1体を対象として破壊する効果モンスター。</summary>
    public static CardData FlipDestroyMonster(MonsterSide side = MonsterSide.Both, int defense = 600) =>
        new(8, "FlipDestroyMonster", CardKind.EffectMonster, 2, 450, defense,
            new CardEffect([new DestroyTargetMonsterAction()], side, MonsterTrigger: MonsterEffectTrigger.Flipped));

    /// <summary>効果を持たない魔法(発動できない)。</summary>
    public static CardData Spell() => new(2, "Spell", CardKind.Spell);

    /// <summary>対象を取らない魔法。</summary>
    public static CardData Spell(params EffectAction[] actions) =>
        new(3, "EffectSpell", CardKind.Spell, Effect: new CardEffect(actions));

    /// <summary>対象のモンスター1体を破壊する魔法。</summary>
    public static CardData DestroyTargetSpell(MonsterSide side = MonsterSide.Opponent) =>
        new(4, "DestroyTargetSpell", CardKind.Spell, Effect: new CardEffect([new DestroyTargetMonsterAction()], side));

    /// <summary>効果を持たない罠(発動できない)。</summary>
    public static CardData Trap() => new(5, "Trap", CardKind.Trap);

    /// <summary>指定したタイミングで発動する罠。</summary>
    public static CardData Trap(TrapTrigger trigger, params EffectAction[] actions) =>
        new(6, $"Trap{trigger}", CardKind.Trap, Effect: new CardEffect(actions, Trigger: trigger));

    /// <summary>相手の攻撃宣言時に、攻撃モンスターを破壊する罠。</summary>
    public static CardData DestroyAttackerTrap() =>
        Trap(TrapTrigger.OpponentAttackDeclared, new DestroyAttackingMonsterAction());

    /// <summary>相手の攻撃宣言時に、その攻撃を無効にする罠。</summary>
    public static CardData NegateAttackTrap() =>
        Trap(TrapTrigger.OpponentAttackDeclared, new NegateAttackAction());

    /// <summary>相手の召喚時に、召喚されたモンスターを破壊する罠。</summary>
    public static CardData DestroySummonedTrap() =>
        Trap(TrapTrigger.OpponentMonsterSummoned, new DestroySummonedMonsterAction());

    /// <summary>
    /// デッキの水増し用。ドローしても召喚・発動できない(効果のない)魔法カードにして、テスト内容に関与させない。
    /// </summary>
    public static CardData Filler() => new(999, "Filler", CardKind.Spell);
}
