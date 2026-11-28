using CardGame.Core.Duels;

namespace CardGame.AI.Strategies;

/// <summary>手札の上限を超えた分として、手札を1枚捨てる。</summary>
public sealed record CpuDiscardAction(int HandIndex) : CpuAction
{
    public override ActionResult ExecuteOn(Duel duel) => duel.Discard(HandIndex);
}
