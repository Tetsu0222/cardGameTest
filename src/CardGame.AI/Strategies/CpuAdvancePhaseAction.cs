using CardGame.Core.Duels;

namespace CardGame.AI.Strategies;

/// <summary>次のフェーズへ進む(エンドフェーズなら相手のターンへ渡す)。</summary>
public sealed record CpuAdvancePhaseAction() : CpuAction
{
    public override ActionResult ExecuteOn(Duel duel) => duel.AdvancePhase();
}
