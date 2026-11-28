using CardGame.Core.Cards;
using CardGame.Core.Duels;

namespace CardGame.AI.Strategies;

/// <summary>発動した効果モンスターの効果の対象を選ぶ。</summary>
public sealed record CpuChooseEffectTargetAction(FieldMonster Target) : CpuAction
{
    public override ActionResult ExecuteOn(Duel duel) => duel.ChooseEffectTarget(Target);
}
