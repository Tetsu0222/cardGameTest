namespace CardGame.Core.Effects;

/// <summary>罠を発動できるタイミング(罠を持つプレイヤーから見た、相手の行動)。</summary>
public enum TrapTrigger
{
    /// <summary>相手のモンスターが攻撃を宣言した時。</summary>
    OpponentAttackDeclared,

    /// <summary>相手がモンスターを召喚(セットを除く)した時。</summary>
    OpponentMonsterSummoned,
}
