using CardGame.Core.Cards;
using CardGame.Core.Duels;

namespace CardGame.AI.Strategies;

/// <summary>
/// 手札のモンスターを通常召喚またはセットする(空いているマスとリリースしたマスのうち一番左)。
/// </summary>
/// <param name="Tributes">アドバンス召喚でリリースするモンスター。リリースしない場合は null。</param>
public sealed record CpuSummonAction(
    int HandIndex,
    BattlePosition Position,
    IReadOnlyList<FieldMonster>? Tributes = null) : CpuAction
{
    public override ActionResult ExecuteOn(Duel duel) => duel.Summon(HandIndex, Position, Tributes);
}
