using CardGame.Core.Cards;

namespace CardGame.Web.Features.DeckEditor;

/// <summary>デッキに入っているカードと枚数(画面表示用)。</summary>
public sealed record DeckEntryView(CardData Card, int Count);
