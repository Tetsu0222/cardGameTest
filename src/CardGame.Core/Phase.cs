namespace CardGame.Core;

/// <summary>ターンのフェーズ(2004年環境: シンクロ導入前)。</summary>
public enum Phase
{
    Draw,
    Standby,
    Main1,
    Battle,
    Main2,
    End,
}
