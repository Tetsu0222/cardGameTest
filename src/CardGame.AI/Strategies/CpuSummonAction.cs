using CardGame.Core.Cards;
using CardGame.Core.Duels;

namespace CardGame.AI.Strategies;

/// <summary>手札のモンスターを通常召喚またはセットする(空いているマスのうち一番左)。</summary>
public sealed record CpuSummonAction(int HandIndex, BattlePosition Position) : CpuAction
{
    public override ActionResult ExecuteOn(Duel duel) => duel.Summon(HandIndex, Position);
}
