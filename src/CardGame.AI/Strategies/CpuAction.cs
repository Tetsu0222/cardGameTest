using CardGame.Core.Duels;

namespace CardGame.AI.Strategies;

/// <summary>
/// CPU が決めた1手。決める処理(<see cref="SimpleCpuStrategy.DecideNextAction"/>)と実行(<see cref="ExecuteOn"/>)を分けることで、
/// 画面側は実行前に「何をするか」を表示でき、将来は実行前に相手の応答(罠など)を挟める。
/// </summary>
public abstract record CpuAction
{
    /// <summary>この手を対戦に対して実行する。</summary>
    public abstract ActionResult ExecuteOn(Duel duel);
}
