using CardGame.Core.Cards;
using CardGame.Core.Duels;

namespace CardGame.AI.Strategies;

/// <summary>モンスターの表示形式を変更する(裏側守備表示なら反転召喚)。</summary>
public sealed record CpuChangePositionAction(FieldMonster Monster) : CpuAction
{
    public override ActionResult ExecuteOn(Duel duel) => duel.ChangePosition(Monster);
}
