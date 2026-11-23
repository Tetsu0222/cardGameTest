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
        ActionError.TributeRequired => "レベル5以上のモンスターは生け贄召喚が必要です(未対応)。",
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
        _ => error.ToString(),
    };
}
