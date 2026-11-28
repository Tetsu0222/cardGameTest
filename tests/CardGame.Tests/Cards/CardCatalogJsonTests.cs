using CardGame.Core.Cards;
using CardGame.Core.Decks;
using CardGame.Core.Effects;

namespace CardGame.Tests.Cards;

public class CardCatalogJsonTests
{
    /// <summary>1枚のカードだけを持つカードデータの JSON を作る。</summary>
    private static string CatalogJson(string cardJson) => $$"""
        {
          "cards": [ {{cardJson}} ],
          "defaultDeck": { "name": "Deck", "entries": [] }
        }
        """;

    private static CardData ParseSingle(string cardJson) => Assert.Single(CardCatalogJson.Parse(CatalogJson(cardJson)).Cards);

    // ---- 読み込み ----

    [Fact]
    public void Parse_ReadsMonster()
    {
        var card = ParseSingle("""{ "id": 1, "name": "M", "kind": "NormalMonster", "level": 4, "attack": 1800, "defense": 1000 }""");

        Assert.Equal(new CardData(1, "M", CardKind.NormalMonster, 4, 1800, 1000), card);
    }

    [Fact]
    public void Parse_ReadsSpellWithTargetEffect()
    {
        var card = ParseSingle("""
            { "id": 2, "name": "S", "kind": "Spell",
              "effect": { "targetSide": "Opponent", "actions": [ { "type": "DestroyTargetMonster" } ] } }
            """);

        Assert.Equal(MonsterSide.Opponent, card.Effect?.TargetSide);
        Assert.IsType<DestroyTargetMonsterAction>(Assert.Single(card.Effect?.Actions ?? []));
    }

    [Fact]
    public void Parse_ReadsTrapWithTrigger()
    {
        var card = ParseSingle("""
            { "id": 3, "name": "T", "kind": "Trap",
              "effect": { "trigger": "OpponentAttackDeclared", "actions": [ { "type": "NegateAttack" } ] } }
            """);

        Assert.Equal(TrapTrigger.OpponentAttackDeclared, card.Effect?.Trigger);
    }

    [Fact]
    public void Parse_ReadsActionParameters()
    {
        var card = ParseSingle("""
            { "id": 4, "name": "S", "kind": "Spell",
              "effect": { "actions": [
                { "type": "DealDamage", "amount": 300 },
                { "type": "DrawCards", "count": 2 },
                { "type": "GainLifePoints", "amount": 500 },
                { "type": "DestroyAllMonsters", "side": "Opponent", "attackPositionOnly": true }
              ] } }
            """);

        EffectAction[] expected =
        [
            new DealDamageAction(300),
            new DrawCardsAction(2),
            new GainLifePointsAction(500),
            new DestroyAllMonstersAction(MonsterSide.Opponent, AttackPositionOnly: true),
        ];
        Assert.Equal(expected, card.Effect?.Actions);
    }

    [Fact]
    public void Parse_ReadsDefaultDeck()
    {
        var catalog = CardCatalogJson.Parse("""
            {
              "cards": [ { "id": 1, "name": "M", "kind": "NormalMonster", "level": 4 } ],
              "defaultDeck": { "name": "初期", "entries": [ { "cardId": 1, "count": 3 } ] }
            }
            """);

        Assert.Equal("初期", catalog.DefaultDeck.Name);
        Assert.Equal(new DeckEntry(1, 3), Assert.Single(catalog.DefaultDeck.Entries));
    }

    // ---- 書き出し ----

    [Fact]
    public void Serialize_ThenParse_KeepsAllCards()
    {
        var original = TestSupport.TestCatalog.Load();

        var json = CardCatalogJson.Serialize(original);
        var reparsed = CardCatalogJson.Parse(json);

        // CardEffect は一覧を参照で比べるため、もう一度 JSON にして中身を比べる
        Assert.Equal(json, CardCatalogJson.Serialize(reparsed));
        Assert.Equal(original.Cards.Count, reparsed.Cards.Count);
    }

    // ---- 読み込めないデータ ----

    [Theory]
    [InlineData("not json")]
    [InlineData("""{ "cards": [] }""")]
    [InlineData("""{ "defaultDeck": { "name": "D", "entries": [] } }""")]
    public void Parse_Throws_WhenStructureInvalid(string json)
    {
        Assert.Throws<InvalidDataException>(() => CardCatalogJson.Parse(json));
    }

    [Fact]
    public void Parse_Throws_WhenUnknownActionType()
    {
        var json = CatalogJson("""
            { "id": 1, "name": "S", "kind": "Spell", "effect": { "actions": [ { "type": "Unknown" } ] } }
            """);

        Assert.Throws<InvalidDataException>(() => CardCatalogJson.Parse(json));
    }

    [Theory]
    [InlineData("""{ "id": 1, "name": "S", "kind": "Spell" }""")] // 効果のない魔法
    [InlineData("""{ "id": 1, "name": "T", "kind": "Trap", "effect": { "actions": [ { "type": "NegateAttack" } ] } }""")] // タイミングのない罠
    [InlineData("""{ "id": 1, "name": "M", "kind": "NormalMonster", "level": 0 }""")] // レベル0
    [InlineData("""{ "id": 0, "name": "M", "kind": "NormalMonster", "level": 4 }""")] // ID が 0
    public void Parse_Throws_WhenCardDefinitionInvalid(string cardJson)
    {
        Assert.Throws<InvalidDataException>(() => CardCatalogJson.Parse(CatalogJson(cardJson)));
    }

    [Fact]
    public void Parse_Throws_WhenCardIdDuplicated()
    {
        var json = """
            {
              "cards": [
                { "id": 1, "name": "A", "kind": "NormalMonster", "level": 4 },
                { "id": 1, "name": "B", "kind": "NormalMonster", "level": 4 }
              ],
              "defaultDeck": { "name": "D", "entries": [] }
            }
            """;

        Assert.Throws<InvalidDataException>(() => CardCatalogJson.Parse(json));
    }
}
