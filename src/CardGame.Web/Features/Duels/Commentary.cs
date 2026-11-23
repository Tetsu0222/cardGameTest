using CardGame.Core.Cards;

namespace CardGame.Web.Features.Duels;

/// <summary>盤面中央に出す実況。</summary>
/// <param name="Card">あわせて表示するカード(発動した魔法・召喚したモンスターなど)。なければ null。</param>
public sealed record Commentary(string Text, CardData? Card = null);
