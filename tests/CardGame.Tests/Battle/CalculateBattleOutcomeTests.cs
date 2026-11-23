using CardGame.Core.Battle;
using CardGame.Core.Cards;
using CardGame.Tests.TestSupport;

namespace CardGame.Tests.Battle;

public class CalculateBattleOutcomeTests
{
    [Theory]
    [InlineData(2000, 1500, false, true, 0, 500)]   // 攻撃側が上: 対象破壊、対象側に差分
    [InlineData(1500, 1500, true, true, 0, 0)]      // 相打ち: 両方破壊、ダメージなし
    [InlineData(1000, 1500, true, false, 500, 0)]   // 攻撃側が下: 攻撃側破壊、攻撃側に差分
    public void Execute_ComparesAttack_WhenTargetInAttackPosition(
        int attackerAttack, int targetAttack,
        bool attackerDestroyed, bool targetDestroyed, int damageToAttacker, int damageToTarget)
    {
        var target = new FieldMonster(TestCards.Monster(attack: targetAttack), BattlePosition.FaceUpAttack);

        var outcome = CalculateBattleOutcome.Execute(attackerAttack, target);

        Assert.Equal(
            new BattleOutcome(attackerDestroyed, targetDestroyed, damageToAttacker, damageToTarget),
            outcome);
    }

    [Theory]
    [InlineData(2000, 1500, false, true, 0)]    // 攻撃力 > 守備力: 対象破壊、ダメージなし
    [InlineData(1500, 1500, false, false, 0)]   // 同値: 何も起きない
    [InlineData(1000, 1500, false, false, 500)] // 攻撃力 < 守備力: 攻撃側に差分(破壊はされない)
    public void Execute_ComparesDefense_WhenTargetInDefensePosition(
        int attackerAttack, int targetDefense,
        bool attackerDestroyed, bool targetDestroyed, int damageToAttacker)
    {
        var target = new FieldMonster(TestCards.Monster(defense: targetDefense), BattlePosition.FaceUpDefense);

        var outcome = CalculateBattleOutcome.Execute(attackerAttack, target);

        Assert.Equal(new BattleOutcome(attackerDestroyed, targetDestroyed, damageToAttacker, 0), outcome);
    }

    [Fact]
    public void Execute_DealsFullAttackToTargetOwner_WhenDirectAttack()
    {
        var outcome = CalculateBattleOutcome.Execute(1800, target: null);

        Assert.Equal(new BattleOutcome(false, false, 0, 1800), outcome);
    }
}
