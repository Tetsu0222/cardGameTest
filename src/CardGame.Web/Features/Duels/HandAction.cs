namespace CardGame.Web.Features.Duels;

public enum HandActionKind
{
    /// <summary>モンスターを攻撃表示で通常召喚する。</summary>
    Summon,

    /// <summary>モンスターを裏側守備表示でセットする。</summary>
    SetMonster,

    /// <summary>魔法を発動する。</summary>
    ActivateSpell,

    /// <summary>魔法・罠を魔法・罠ゾーンにセットする。</summary>
    SetSpellTrap,

    /// <summary>手札の上限を超えた分として捨てる(エンドフェーズ)。</summary>
    Discard,
}

/// <summary>手札のカードに対する操作。</summary>
public sealed record HandAction(int HandIndex, HandActionKind Kind);
