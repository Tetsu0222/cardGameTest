using CardGame.Core.Duels;

namespace CardGame.Web.Features.Duels;

/// <summary>対戦ログを画面表示用の文言に変換する。</summary>
public static class DuelLogFormatter
{
    public static string Format(DuelLogEntry entry) => entry.Kind switch
    {
        DuelLogKind.TurnStarted => $"ターン{entry.Turn}: {entry.PlayerName}のターン",
        DuelLogKind.Summoned => $"{entry.PlayerName}は「{entry.CardName}」を召喚",
        // セットしたカード名は相手に見えないため表示しない
        DuelLogKind.Set => $"{entry.PlayerName}はモンスターをセット",
        DuelLogKind.Attacked => $"{entry.PlayerName}の「{entry.CardName}」が{TargetText(entry)}に攻撃",
        DuelLogKind.DirectAttacked => $"{entry.PlayerName}の「{entry.CardName}」がダイレクトアタック",
        DuelLogKind.Flipped => $"{entry.PlayerName}の「{entry.CardName}」が表になった",
        DuelLogKind.Damaged => $"{entry.PlayerName}に {entry.Amount} ダメージ",
        DuelLogKind.Destroyed => $"{entry.PlayerName}の「{entry.CardName}」が破壊された",
        DuelLogKind.DeckOut => $"{entry.PlayerName}はデッキ切れでドローできない",
        DuelLogKind.Won => $"{entry.PlayerName}の勝利",
        _ => entry.Kind.ToString(),
    };

    private static string TargetText(DuelLogEntry entry) =>
        entry.TargetCardName is null ? "裏側守備モンスター" : $"「{entry.TargetCardName}」";
}
