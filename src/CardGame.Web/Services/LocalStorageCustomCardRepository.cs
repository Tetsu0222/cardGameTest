using CardGame.Core.Cards;
using Microsoft.JSInterop;

namespace CardGame.Web.Services;

/// <summary>
/// プレイヤーが作ったカードを、ブラウザの保存領域(localStorage)に保存する。
/// 保存したカードはこのブラウザだけに残り、他の端末やブラウザとは共有されない。
/// </summary>
public sealed class LocalStorageCustomCardRepository(IJSRuntime js) : ICustomCardRepository, IAsyncDisposable
{
    private const string StorageKey = "cardGame.customCards";
    private const string ModulePath = "./js/storage.js";

    private Task<IJSObjectReference>? _module;

    public async Task<IReadOnlyList<CardData>> LoadAsync()
    {
        var module = await GetModuleAsync();
        var json = await module.InvokeAsync<string?>("getItem", StorageKey);
        return json is null ? [] : CustomCardsJson.TryParse(json) ?? [];
    }

    public async Task<bool> SaveAsync(IReadOnlyList<CardData> cards)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<bool>("setItem", StorageKey, CustomCardsJson.Serialize(cards));
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            await (await _module).DisposeAsync();
        }
    }

    private Task<IJSObjectReference> GetModuleAsync() =>
        _module ??= js.InvokeAsync<IJSObjectReference>("import", ModulePath).AsTask();
}
