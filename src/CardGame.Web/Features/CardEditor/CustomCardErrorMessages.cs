using CardGame.Core.Cards;

namespace CardGame.Web.Features.CardEditor;

/// <summary>作成したカードがルールに合っていない理由を、画面表示用の文言に変換する。</summary>
public static class CustomCardErrorMessages
{
    public static string ToMessage(CustomCardError error, CardCreationOptions options) => error switch
    {
        CustomCardError.InvalidId => "カードの番号が正しくありません。作り直してください。",
        CustomCardError.NameRequired => "カード名を入力してください。",
        CustomCardError.NameTooLong => $"カード名は{options.MaxNameLength}文字までです。",
        CustomCardError.UnsupportedKind => "この種類のカードは作れません。",
        CustomCardError.LevelOutOfRange => $"レベルは{options.MinLevel}〜{options.MaxLevel}の範囲で選んでください。",
        CustomCardError.StatOutOfRange => $"攻撃力・守備力は0〜{options.MaxStat}({options.StatStep}刻み)で選んでください。",
        CustomCardError.NoEffect => "効果を1つ以上追加してください。",
        CustomCardError.TooManyActions => $"効果は{options.MaxEffectActions}つまでです。",
        CustomCardError.AmountOutOfRange => "効果の数値が選べる範囲を超えています。",
        CustomCardError.TrapCannotTarget => "罠に対象を取る効果は入れられません。",
        CustomCardError.InvalidCombination => "効果の組み合わせが成り立っていません(発動タイミングに合わない効果が入っていないか確認してください)。",
        _ => error.ToString(),
    };
}
