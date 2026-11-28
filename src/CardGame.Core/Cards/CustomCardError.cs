namespace CardGame.Core.Cards;

/// <summary>作成したカードが、カード作成のルールに合っていない理由。</summary>
public enum CustomCardError
{
    /// <summary>自作カード用の ID の範囲外。</summary>
    InvalidId,
    NameRequired,
    NameTooLong,
    UnsupportedKind,
    LevelOutOfRange,

    /// <summary>攻撃力・守備力が範囲外、または刻みに合っていない。</summary>
    StatOutOfRange,

    NoEffect,
    TooManyActions,

    /// <summary>ダメージ量・回復量・ドロー枚数が範囲外、または刻みに合っていない。</summary>
    AmountOutOfRange,

    /// <summary>罠が対象を取る効果を持っている(罠の対象選択は未対応)。</summary>
    TrapCannotTarget,

    /// <summary>部品の組み合わせが成り立っていない(発動タイミングと部品が合わないなど)。</summary>
    InvalidCombination,
}
