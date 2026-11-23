using CardGame.Core.Duels;

namespace CardGame.Web.Features.Duels;

/// <summary>操作が拒否された理由を画面表示用の文言に変換する。</summary>
public static class ActionErrorMessages
{
    public static string ToMessage(ActionError error) => error switch
    {
        ActionError.None => "",
        ActionError.DuelFinished => "対戦は終了しています。",
        ActionError.WrongPhase => "今のフェーズではその操作はできません。",
        ActionError.InvalidHandIndex => "そのカードは選べません。",
        ActionError.NotAMonster => "モンスターカードではありません。",
        ActionError.InvalidPosition => "その表示形式では召喚できません。",
        ActionError.NotEnoughTributes => "リリースできるモンスターが足りません(レベル5・6は1体、レベル7以上は2体)。",
        ActionError.WrongTributeCount => "リリースするモンスターの数が違います。",
        ActionError.InvalidTribute => "自分の場のモンスターを、重ならないように選んでください。",
        ActionError.NormalSummonAlreadyUsed => "通常召喚はこのターンすでに行っています。",
        ActionError.MonsterZoneFull => "モンスターゾーンに空きがありません。",
        ActionError.InvalidSlot => "そのマスは選べません。",
        ActionError.SlotOccupied => "そのマスにはすでにモンスターがいます。",
        ActionError.CannotAttackOnFirstTurn => "先攻の最初のターンは攻撃できません。",
        ActionError.AttackerNotOnYourField => "自分の場のモンスターを選んでください。",
        ActionError.AttackerNotInAttackPosition => "攻撃表示のモンスターでのみ攻撃できます。",
        ActionError.AlreadyAttacked => "このモンスターはこのターンすでに攻撃しています。",
        ActionError.InvalidTarget => "相手の場のモンスターを選んでください。",
        ActionError.DirectAttackNotAllowed => "相手の場にモンスターがいるため、ダイレクトアタックできません。",
        ActionError.NotASpell => "魔法カードではありません。",
        ActionError.NotASpellOrTrap => "魔法・罠カードではありません。",
        ActionError.SpellTrapZoneFull => "魔法・罠ゾーンに空きがありません。",
        ActionError.CardNotOnYourField => "自分の場のカードを選んでください。",
        ActionError.TargetRequired => "効果の対象を選んでください。",
        ActionError.TargetNotAllowed => "このカードは対象を取りません。",
        ActionError.NoValidTarget => "効果の対象にできるモンスターがいません。",
        ActionError.ActivationConditionNotMet => "今はこのカードを発動できません。",
        ActionError.ResponsePending => "相手が罠を発動するかを選んでいます。",
        ActionError.NoResponseWindow => "今は罠を発動できません。",
        ActionError.NotATrap => "罠カードではありません。",
        ActionError.TrapSetThisTurn => "罠はセットしたターンには発動できません。",
        ActionError.MonsterNotOnYourField => "自分の場のモンスターを選んでください。",
        ActionError.SummonedThisTurn => "召喚・セットしたターンには表示形式を変更できません。",
        ActionError.PositionAlreadyChanged => "表示形式の変更は1ターンに1回までです。",
        _ => error.ToString(),
    };
}
