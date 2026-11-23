using CardGame.Core.Cards;

namespace CardGame.Web.Features.Duels;

/// <summary>
/// 対象を選ぶ途中の魔法の発動。手札から発動する場合は <see cref="HandIndex"/>、
/// セットしたカードを発動する場合は <see cref="SetCard"/> を持つ。
/// </summary>
public sealed record PendingSpellActivation(CardData Card, int? HandIndex, SpellTrapCard? SetCard);
