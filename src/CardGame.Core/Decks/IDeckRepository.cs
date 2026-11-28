namespace CardGame.Core.Decks;

/// <summary>
/// プレイヤーのデッキの保存先。Core は保存方法を知らない(実装は Web のブラウザ保存領域など)。
/// </summary>
public interface IDeckRepository
{
    /// <summary>保存してあるデッキ。保存していない、または読めない場合は null。</summary>
    Task<DeckList?> LoadAsync();

    /// <summary>デッキを保存する。保存領域が使えないなどで保存できなかった場合は false。</summary>
    Task<bool> SaveAsync(DeckList deck);
}
