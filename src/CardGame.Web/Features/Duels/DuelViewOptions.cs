namespace CardGame.Web.Features.Duels;

/// <summary>対戦画面の表示に関する設定(appsettings.json の "DuelView")。ゲームのルールには影響しない。</summary>
public sealed class DuelViewOptions
{
    /// <summary>CPU の1手ごとの待ち時間(ミリ秒)。行動の表示と、結果の表示のそれぞれで待つ。</summary>
    public int CpuActionDelayMilliseconds { get; init; } = 700;
}
