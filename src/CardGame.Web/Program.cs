using CardGame.Web;
using CardGame.Web.Features.DeckEditor;
using CardGame.Web.Features.Duels;
using CardGame.Web.Shared;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddGameData(new Uri(builder.HostEnvironment.BaseAddress));
builder.Services.AddDuelServices(builder.Configuration);
builder.Services.AddDeckEditorServices();

var host = builder.Build();

// 画面を表示する前に、カードデータと保存してあるデッキを読み込む
await host.Services.GetRequiredService<GameData>().InitializeAsync();

await host.RunAsync();
