using CardGame.Core.Cards;
using CardGame.Core.Duels;

namespace CardGame.AI.Strategies;

/// <summary>モンスターで攻撃する。</summary>
/// <param name="Target">攻撃対象。null はダイレクトアタック。</param>
public sealed record CpuAttackAction(FieldMonster Attacker, FieldMonster? Target) : CpuAction
{
    public override ActionResult ExecuteOn(Duel duel) => duel.Attack(Attacker, Target);
}
