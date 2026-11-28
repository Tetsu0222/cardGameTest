namespace CardGame.Core.Cards;

/// <summary>プレイヤーが作ったカードの保存先。Core は保存方法を知らない(実装は Web のブラウザ保存領域など)。</summary>
public interface ICustomCardRepository
{
    /// <summary>保存してあるカード。保存していない、または読めない場合は空。</summary>
    Task<IReadOnlyList<CardData>> LoadAsync();

    /// <summary>作ったカードの一覧をまとめて保存する。保存できなかった場合は false。</summary>
    Task<bool> SaveAsync(IReadOnlyList<CardData> cards);
}
