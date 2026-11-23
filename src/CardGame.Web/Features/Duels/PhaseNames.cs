using CardGame.Core.Turns;

namespace CardGame.Web.Features.Duels;

/// <summary>フェーズの表示名。</summary>
public static class PhaseNames
{
    public static IReadOnlyList<Phase> All { get; } =
        [Phase.Draw, Phase.Standby, Phase.Main1, Phase.Battle, Phase.Main2, Phase.End];

    public static string ToDisplayName(Phase phase) => phase switch
    {
        Phase.Draw => "ドロー",
        Phase.Standby => "スタンバイ",
        Phase.Main1 => "メイン1",
        Phase.Battle => "バトル",
        Phase.Main2 => "メイン2",
        Phase.End => "エンド",
        _ => phase.ToString(),
    };
}
