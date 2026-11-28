using CardGame.Core.Cards;
using CardGame.Core.Effects;
using CardGame.Core.Players;

namespace CardGame.Core.Duels;

/// <summary>発動した効果モンスターの効果(処理待ち、または対象の選択待ち)。</summary>
/// <param name="Controller">効果を持つモンスターのプレイヤー(効果を使うプレイヤー)。ターンプレイヤーとは限らない。</param>
/// <param name="Source">効果を持つモンスター。効果の処理時には場を離れていることもある。</param>
public sealed record PendingMonsterEffect(Player Controller, FieldMonster Source, CardEffect Effect);
