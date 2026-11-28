using CardGame.Core.Cards;
using CardGame.Core.Decks;
using CardGame.Core.Duels;
using CardGame.Core.Effects;
using CardGame.Tests.TestSupport;

namespace CardGame.Tests.Cards;

/// <summary>プレイヤーが作るカードの検証・ID の採番・保存形式・カードデータへの追加。</summary>
public class CustomCardTests
{
    private const int Id = ValidateCustomCard.FirstCustomCardId;

    private static readonly CardCreationOptions Options = new();

    private static CardData Monster(int level = 4, int attack = 1500, int defense = 1200, string name = "自作モンスター") =>
        new(Id, name, CardKind.NormalMonster, level, attack, defense);

    private static CardData Spell(params EffectAction[] actions) =>
        new(Id, "自作魔法", CardKind.Spell, Effect: new CardEffect(actions));

    private static IReadOnlyList<CustomCardError> Validate(CardData card) => ValidateCustomCard.Execute(card, Options);

    // ---- 検証: 成り立つカード ----

    [Fact]
    public void Validate_AcceptsMonsterWithinLimits()
    {
        Assert.Empty(Validate(Monster(level: Options.MaxLevel, attack: Options.MaxStat, defense: 0)));
    }

    [Fact]
    public void Validate_AcceptsSpellCombiningActions()
    {
        Assert.Empty(Validate(Spell(new DealDamageAction(Options.MaxLifeAmount), new DrawCardsAction(Options.MaxDrawCount))));
    }

    [Fact]
    public void Validate_AcceptsTrapWithTrigger()
    {
        var trap = new CardData(Id, "自作罠", CardKind.Trap,
            Effect: new CardEffect([new DestroyAttackingMonsterAction(), new DealDamageAction(500)], Trigger: TrapTrigger.OpponentAttackDeclared));

        Assert.Empty(Validate(trap));
    }

    // ---- 検証: 名前・ID ----

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_RequiresName(string name)
    {
        Assert.Contains(CustomCardError.NameRequired, Validate(Monster(name: name)));
    }

    [Fact]
    public void Validate_RejectsTooLongName()
    {
        Assert.Contains(CustomCardError.NameTooLong, Validate(Monster(name: new string('あ', Options.MaxNameLength + 1))));
        Assert.Empty(Validate(Monster(name: new string('あ', Options.MaxNameLength))));
    }

    [Fact]
    public void Validate_RejectsBuiltInIdRange()
    {
        var card = Monster() with { Id = ValidateCustomCard.FirstCustomCardId - 1 };

        Assert.Contains(CustomCardError.InvalidId, Validate(card));
    }

    // ---- 検証: モンスター ----

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void Validate_RejectsLevelOutOfRange(int level)
    {
        Assert.Contains(CustomCardError.LevelOutOfRange, Validate(Monster(level: level)));
    }

    [Theory]
    [InlineData(-100, 1000)]
    [InlineData(4100, 1000)]
    [InlineData(1050, 1000)] // 刻み(100)に合わない
    [InlineData(1000, 1050)]
    public void Validate_RejectsStatsOutOfRangeOrOffStep(int attack, int defense)
    {
        Assert.Contains(CustomCardError.StatOutOfRange, Validate(Monster(attack: attack, defense: defense)));
    }

    [Fact]
    public void Validate_RequiresEffectForEffectMonster()
    {
        Assert.Contains(CustomCardError.NoEffect, Validate(Monster() with { Kind = CardKind.EffectMonster }));
    }

    [Fact]
    public void Validate_AcceptsEffectMonster_WithTriggerAndEffect()
    {
        var card = Monster() with
        {
            Kind = CardKind.EffectMonster,
            Effect = new CardEffect([new DestroyTargetMonsterAction()], MonsterSide.Both, MonsterTrigger: MonsterEffectTrigger.Flipped),
        };

        Assert.Empty(Validate(card));
    }

    [Fact]
    public void Validate_ChecksStatsOfEffectMonster()
    {
        var card = Monster(attack: 4100) with
        {
            Kind = CardKind.EffectMonster,
            Effect = new CardEffect([new DealDamageAction(300)], MonsterTrigger: MonsterEffectTrigger.Summoned),
        };

        Assert.Contains(CustomCardError.StatOutOfRange, Validate(card));
    }

    // ---- 検証: 効果 ----

    [Fact]
    public void Validate_RequiresEffectForSpell()
    {
        Assert.Contains(CustomCardError.NoEffect, Validate(new CardData(Id, "魔法", CardKind.Spell)));
        Assert.Contains(CustomCardError.NoEffect, Validate(Spell()));
    }

    [Fact]
    public void Validate_RejectsTooManyActions()
    {
        var actions = Enumerable.Repeat<EffectAction>(new DrawCardsAction(1), Options.MaxEffectActions + 1).ToArray();

        Assert.Contains(CustomCardError.TooManyActions, Validate(Spell(actions)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2100)]
    [InlineData(150)]
    public void Validate_RejectsLifeAmountOutOfRangeOrOffStep(int amount)
    {
        Assert.Contains(CustomCardError.AmountOutOfRange, Validate(Spell(new DealDamageAction(amount))));
        Assert.Contains(CustomCardError.AmountOutOfRange, Validate(Spell(new GainLifePointsAction(amount))));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void Validate_RejectsDrawCountOutOfRange(int count)
    {
        Assert.Contains(CustomCardError.AmountOutOfRange, Validate(Spell(new DrawCardsAction(count))));
    }

    [Fact]
    public void Validate_RejectsTargetingTrap()
    {
        var trap = new CardData(Id, "罠", CardKind.Trap,
            Effect: new CardEffect([new DestroyTargetMonsterAction()], MonsterSide.Opponent, TrapTrigger.OpponentAttackDeclared));

        Assert.Contains(CustomCardError.TrapCannotTarget, Validate(trap));
    }

    [Fact]
    public void Validate_RejectsInvalidCombination()
    {
        // 召喚時の罠に、攻撃宣言時専用の部品を入れている
        var trap = new CardData(Id, "罠", CardKind.Trap,
            Effect: new CardEffect([new NegateAttackAction()], Trigger: TrapTrigger.OpponentMonsterSummoned));

        Assert.Equal([CustomCardError.InvalidCombination], Validate(trap));
    }

    [Fact]
    public void Validate_UsesConfiguredLimits()
    {
        var strict = new CardCreationOptions { MaxStat = 2000, StatStep = 500 };

        Assert.Empty(ValidateCustomCard.Execute(Monster(attack: 1500, defense: 500), strict));
        Assert.Contains(CustomCardError.StatOutOfRange, ValidateCustomCard.Execute(Monster(attack: 2500), strict));
    }

    // ---- ID の採番 ----

    [Fact]
    public void IssueId_StartsAtFirstCustomId_WhenNoCustomCards()
    {
        Assert.Equal(ValidateCustomCard.FirstCustomCardId, IssueCustomCardId.Execute(TestCatalog.Load().Cards));
    }

    [Fact]
    public void IssueId_ContinuesAfterLargestCustomId()
    {
        CardData[] cards = [Monster() with { Id = 10_000 }, Monster() with { Id = 10_005 }, Monster() with { Id = 3 }];

        Assert.Equal(10_006, IssueCustomCardId.Execute(cards));
    }

    // ---- 保存形式 ----

    [Fact]
    public void Json_RoundTrips()
    {
        CardData[] cards = [Monster(), Spell(new DealDamageAction(300)) with { Id = Id + 1 }];

        var json = CustomCardsJson.Serialize(cards);
        var restored = CustomCardsJson.TryParse(json);

        Assert.NotNull(restored);
        Assert.Equal(json, CustomCardsJson.Serialize(restored));
    }

    [Theory]
    [InlineData("broken")]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("[null]")]
    public void Json_TryParse_ReturnsNull_WhenBroken(string json)
    {
        Assert.Null(CustomCardsJson.TryParse(json));
    }

    // ---- カードデータへの追加 ----

    [Fact]
    public void WithAdditionalCards_AddsCardsAndKeepsDefaultDeck()
    {
        var catalog = TestCatalog.Load();

        var merged = catalog.WithAdditionalCards([Monster()]);

        Assert.Equal(catalog.Cards.Count + 1, merged.Cards.Count);
        Assert.Equal("自作モンスター", merged.Find(Id)?.Name);
        Assert.Same(catalog.DefaultDeck, merged.DefaultDeck);
        Assert.Null(catalog.Find(Id)); // 元のカードデータは変わらない
    }

    [Fact]
    public void WithAdditionalCards_Throws_WhenIdDuplicated()
    {
        var catalog = TestCatalog.Load();

        Assert.Throws<ArgumentException>(() => catalog.WithAdditionalCards([Monster() with { Id = catalog.Cards[0].Id }]));
    }

    [Fact]
    public void DeckWithCustomCard_IsValid_WhenCatalogIncludesIt()
    {
        var catalog = TestCatalog.Load().WithAdditionalCards([Monster()]);
        var deck = new DeckList("Deck", [.. catalog.DefaultDeck.Entries.Skip(1), new DeckEntry(Id, 3)]);

        Assert.Empty(ValidateDeck.Execute(deck, catalog, new GameRulesOptions()));
    }
}
