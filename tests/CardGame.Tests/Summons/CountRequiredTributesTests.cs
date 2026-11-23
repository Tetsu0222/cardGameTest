using CardGame.Core.Summons;
using CardGame.Tests.TestSupport;

namespace CardGame.Tests.Summons;

public class CountRequiredTributesTests
{
    [Theory]
    [InlineData(1, 0)]
    [InlineData(4, 0)]
    [InlineData(5, 1)]
    [InlineData(6, 1)]
    [InlineData(7, 2)]
    [InlineData(8, 2)]
    [InlineData(12, 2)]
    public void Execute_ReturnsTributeCountByLevel(int level, int expected)
    {
        Assert.Equal(expected, CountRequiredTributes.Execute(TestCards.Monster(level: level)));
    }
}
