using CardGame.Core.Duels;

namespace CardGame.AI.Strategies;

/// <summary>相手の行動に対して罠を発動せずに見送る。</summary>
public sealed record CpuPassResponseAction() : CpuAction
{
    public override ActionResult ExecuteOn(Duel duel) => duel.PassResponse();
}
