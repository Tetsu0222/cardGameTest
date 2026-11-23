using CardGame.Core.Cards;
using CardGame.Core.Effects;
using CardGame.Core.Players;

namespace CardGame.Core.Duels;

/// <summary>
/// 相手の行動(攻撃宣言・召喚)に対して、罠を発動するかを決める機会。
/// 開いている間は、<see cref="Responder"/> が罠を発動するか見送るまで、他の操作はできない。
/// </summary>
/// <param name="Trigger">きっかけになった行動。</param>
/// <param name="Responder">罠を発動できるプレイヤー(ターンプレイヤーの相手)。</param>
/// <param name="TriggerMonster">攻撃を宣言したモンスター、または召喚されたモンスター。</param>
/// <param name="AttackTarget">攻撃対象。ダイレクトアタック・召喚では null。</param>
public sealed record ResponseWindow(
    TrapTrigger Trigger,
    Player Responder,
    FieldMonster TriggerMonster,
    FieldMonster? AttackTarget);
