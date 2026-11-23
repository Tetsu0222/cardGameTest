using CardGame.Core.Cards;
using CardGame.Core.Duels;

namespace CardGame.AI.Strategies;

/// <summary>相手の行動に応じて、セットしてある罠を発動する。</summary>
/// <param name="Target">対象。対象を取らない罠では null。</param>
public sealed record CpuActivateTrapAction(SpellTrapCard Trap, FieldMonster? Target) : CpuAction
{
    public override ActionResult ExecuteOn(Duel duel) => duel.ActivateTrap(Trap, Target);
}
