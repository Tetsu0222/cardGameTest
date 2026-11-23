namespace CardGame.Core.Duels;

/// <summary>
/// ゲームバランスに関わる設定値。依存のない素のクラス。
/// <c>IOptions&lt;T&gt;</c> へのバインドは Web の Program.cs のみで行う。
/// </summary>
public sealed class GameRulesOptions
{
    public int InitialLifePoints { get; init; } = 8000;
    public int InitialHandSize { get; init; } = 5;

    /// <summary>先攻の最初のターンはドローしない。</summary>
    public bool FirstPlayerSkipsFirstDraw { get; init; } = true;
}
