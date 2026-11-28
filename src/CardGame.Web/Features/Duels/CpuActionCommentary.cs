using CardGame.AI.Strategies;
using CardGame.Core.Cards;
using CardGame.Core.Duels;
using CardGame.Core.Players;

namespace CardGame.Web.Features.Duels;

/// <summary>CPU がこれから行う1手を、実況の文言にする。実行前(手札のカードや表示形式が変わる前)に呼ぶ。</summary>
public static class CpuActionCommentary
{
    /// <returns>実況。フェーズ移動など、実況しない手は null。</returns>
    /// <param name="duel">対戦の状態(効果の対象を選ぶ手で、どの効果かを調べるのに使う)。</param>
    public static Commentary? Create(CpuAction action, Player cpu, Duel duel) => action switch
    {
        CpuChooseEffectTargetAction choose when duel.PendingEffectTarget is { } pending =>
            new Commentary($"{cpu.Name}の「{pending.Source.Card.Name}」の効果:{MonsterName(choose.Target)}を対象に選択", pending.Source.Card),

        CpuSummonAction { Position: BattlePosition.FaceDownDefense } summon =>
            new Commentary($"{cpu.Name}は{TributePrefix(summon.Tributes)}モンスターを{(HasTributes(summon.Tributes) ? "アドバンスセット" : "セット")}"),

        CpuSummonAction summon when HandCard(cpu, summon.HandIndex) is { } card =>
            new Commentary($"{cpu.Name}は{TributePrefix(summon.Tributes)}「{card.Name}」を{(HasTributes(summon.Tributes) ? "アドバンス召喚" : "召喚")}", card),

        CpuActivateSpellAction spell when HandCard(cpu, spell.HandIndex) is { } card =>
            new Commentary($"{cpu.Name}は「{card.Name}」を発動{TargetSuffix(spell.Target)}", card),

        CpuSetSpellTrapAction =>
            new Commentary($"{cpu.Name}は魔法・罠カードをセット"),

        CpuActivateTrapAction trap =>
            new Commentary($"{cpu.Name}は罠「{trap.Trap.Card.Name}」を発動", trap.Trap.Card),

        CpuChangePositionAction change => change.Monster.Position switch
        {
            BattlePosition.FaceDownDefense => new Commentary($"{cpu.Name}は「{change.Monster.Card.Name}」を反転召喚", change.Monster.Card),
            BattlePosition.FaceUpAttack => new Commentary($"{cpu.Name}は「{change.Monster.Card.Name}」を守備表示に変更"),
            _ => new Commentary($"{cpu.Name}は「{change.Monster.Card.Name}」を攻撃表示に変更"),
        },

        CpuDiscardAction discard when HandCard(cpu, discard.HandIndex) is { } card =>
            new Commentary($"{cpu.Name}は手札の「{card.Name}」を捨てた", card),

        CpuAttackAction { Target: null } attack =>
            new Commentary($"{cpu.Name}の「{attack.Attacker.Card.Name}」のダイレクトアタック"),

        CpuAttackAction attack when attack.Target is { } target =>
            new Commentary($"{cpu.Name}の「{attack.Attacker.Card.Name}」が{MonsterName(target)}に攻撃"),

        _ => null,
    };

    private static CardData? HandCard(Player cpu, int handIndex) =>
        handIndex >= 0 && handIndex < cpu.Hand.Count ? cpu.Hand[handIndex] : null;

    private static bool HasTributes(IReadOnlyList<FieldMonster>? tributes) => tributes is { Count: > 0 };

    /// <summary>「「A」と「B」をリリースし、」のような前置き。リリースしない場合は空。</summary>
    private static string TributePrefix(IReadOnlyList<FieldMonster>? tributes) =>
        tributes is { Count: > 0 } list ? string.Join("と", list.Select(t => $"「{t.Card.Name}」")) + "をリリースし、" : "";

    private static string TargetSuffix(FieldMonster? target) =>
        target is null ? "" : $"({MonsterName(target)}を対象)";

    private static string MonsterName(FieldMonster monster) =>
        monster.Position == BattlePosition.FaceDownDefense ? "裏側守備モンスター" : $"「{monster.Card.Name}」";
}
