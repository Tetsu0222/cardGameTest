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

    /// <summary>エンドフェーズに持っていられる手札の上限。超えた分は捨てる。</summary>
    public int MaxHandSize { get; init; } = 6;

    /// <summary>デッキの最小枚数。</summary>
    public int MinDeckSize { get; init; } = 40;

    /// <summary>デッキの最大枚数。</summary>
    public int MaxDeckSize { get; init; } = 60;

    /// <summary>同じカードをデッキに入れられる最大枚数。</summary>
    public int MaxCopiesPerCard { get; init; } = 3;
}
