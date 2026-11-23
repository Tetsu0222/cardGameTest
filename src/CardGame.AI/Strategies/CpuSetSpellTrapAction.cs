using CardGame.Core.Duels;

namespace CardGame.AI.Strategies;

/// <summary>手札の魔法・罠をセットする(空いているマスのうち一番左)。</summary>
public sealed record CpuSetSpellTrapAction(int HandIndex) : CpuAction
{
    public override ActionResult ExecuteOn(Duel duel) => duel.SetSpellTrap(HandIndex);
}
