namespace CardGame.Core.Effects;

/// <summary>効果モンスターの効果が発動するタイミング。発動は強制(条件を満たせば必ず発動する)。</summary>
public enum MonsterEffectTrigger
{
    /// <summary>リバース。裏側から表になった時(反転召喚した時、攻撃されて表になった時)。</summary>
    Flipped,

    /// <summary>通常召喚(アドバンス召喚を含む)に成功した時。セットでは発動しない。</summary>
    Summoned,
}
