using System.Text.Json.Serialization;

namespace CardGame.Core.Effects;

/// <summary>
/// 効果部品の基底。部品の種類を増やすときは派生 record を追加し、
/// <see cref="ResolveCardEffect"/>・<see cref="ValidateCardEffect"/>・下の <see cref="JsonDerivedTypeAttribute"/>(JSON での種類名)と、
/// Web の CardEffectText(説明文)と、カード作成画面の EffectPartType / EffectPartDraft / EffectPartChoices(選択肢)に処理を加える。
/// </summary>
/// <remarks>JSON では <c>"type"</c> に種類名を書く(例: <c>{ "type": "DealDamage", "amount": 600 }</c>)。</remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(DrawCardsAction), "DrawCards")]
[JsonDerivedType(typeof(DealDamageAction), "DealDamage")]
[JsonDerivedType(typeof(GainLifePointsAction), "GainLifePoints")]
[JsonDerivedType(typeof(DestroyTargetMonsterAction), "DestroyTargetMonster")]
[JsonDerivedType(typeof(DestroyAllMonstersAction), "DestroyAllMonsters")]
[JsonDerivedType(typeof(DestroyAttackingMonsterAction), "DestroyAttackingMonster")]
[JsonDerivedType(typeof(NegateAttackAction), "NegateAttack")]
[JsonDerivedType(typeof(DestroySummonedMonsterAction), "DestroySummonedMonster")]
public abstract record EffectAction;
