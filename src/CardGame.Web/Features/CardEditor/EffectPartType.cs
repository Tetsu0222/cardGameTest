namespace CardGame.Web.Features.CardEditor;

/// <summary>カード作成画面で選べる効果部品の種類。Core の効果部品(EffectAction の派生)と1対1で対応する。</summary>
public enum EffectPartType
{
    DrawCards,
    DealDamage,
    GainLifePoints,
    DestroyTargetMonster,
    DestroyAllMonsters,
    DestroyAttackingMonster,
    NegateAttack,
    DestroySummonedMonster,
}
