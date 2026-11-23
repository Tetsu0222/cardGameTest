namespace CardGame.Core.Effects;

/// <summary>
/// 効果部品の基底。部品の種類を増やすときは派生 record を追加し、
/// <see cref="ResolveCardEffect"/>・<see cref="ValidateCardEffect"/> に処理を加える。
/// </summary>
public abstract record EffectAction;
