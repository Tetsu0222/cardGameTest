using CardGame.Core.Decks;
using Microsoft.JSInterop;

namespace CardGame.Web.Services;

/// <summary>
/// プレイヤーのデッキを、ブラウザの保存領域(localStorage)に保存する。
/// 保存したデッキはこのブラウザだけに残り、他の端末やブラウザとは共有されない。
/// </summary>
public sealed class LocalStorageDeckRepository(IJSRuntime js) : IDeckRepository, IAsyncDisposable
{
    private const string StorageKey = "cardGame.playerDeck";
    private const string ModulePath = "./js/storage.js";

    private Task<IJSObjectReference>? _module;

    public async Task<DeckList?> LoadAsync()
    {
        var module = await GetModuleAsync();
        var json = await module.InvokeAsync<string?>("getItem", StorageKey);
        return json is null ? null : DeckListJson.TryParse(json);
    }

    public async Task<bool> SaveAsync(DeckList deck)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<bool>("setItem", StorageKey, DeckListJson.Serialize(deck));
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
