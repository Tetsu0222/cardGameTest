using CardGame.Core.Cards;
using CardGame.Core.Duels;

namespace CardGame.AI.Strategies;

/// <summary>手札の魔法を発動する。</summary>
/// <param name="Target">対象。対象を取らない魔法では null。</param>
public sealed record CpuActivateSpellAction(int HandIndex, FieldMonster? Target) : CpuAction
{
    public override ActionResult ExecuteOn(Duel duel) => duel.ActivateSpellFromHand(HandIndex, Target);
}
