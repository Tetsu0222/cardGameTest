using CardGame.Core.Cards;

namespace CardGame.Web.Features.Duels.Components;

/// <summary>手札から召喚・セットする操作の内容。</summary>
public sealed record SummonRequest(int HandIndex, BattlePosition Position);
