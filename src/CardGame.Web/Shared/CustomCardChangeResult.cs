namespace CardGame.Web.Shared;

/// <summary>作成したカードの保存・削除の結果。</summary>
public enum CustomCardChangeResult
{
    Succeeded,

    /// <summary>カード作成のルールに合っていない。</summary>
    Invalid,

    /// <summary>保存したデッキに入っているため削除できない。</summary>
    UsedInDeck,

    /// <summary>ブラウザの保存領域に保存できなかった。</summary>
    StorageFailed,
}
