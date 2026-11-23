using CardGame.Core.Cards;

namespace CardGame.Web.Features.Duels;

/// <summary>手札から召喚・セットする操作の内容。</summary>
public sealed record SummonRequest(int HandIndex, BattlePosition Position);
