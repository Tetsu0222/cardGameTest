# cardGame プロジェクト AIコンテキスト

遊戯王風（2004年環境・シンクロ導入前）の1人用カードゲーム Web アプリ。
C# / .NET 10 / Blazor WebAssembly（スタンドアロン）。サーバーなしでブラウザ内で完結する。

このファイルは薄い参照ハブとして保つ。詳細ルールは外部ファイルに置く。

## 必ず読むファイル

- `docs/coding_guidelines.md` — 構成・命名・Blazor/テスト/設定の規約。実装前に必ず参照する。

## 構成（概要）

- `src/CardGame.Core` — ルールエンジン。UI・ブラウザ非依存。
- `src/CardGame.AI` — CPU 思考（Core にのみ依存）。
- `src/CardGame.Web` — Blazor WASM。表示と入力受付のみ。
- `tests/CardGame.Tests` — xUnit。Core / AI を検証。

## 開発環境

ローカルに .NET SDK は入れていない。**dotnet コマンドは Docker 経由で実行する。**

```
docker compose run --rm dotnet dotnet build
docker compose run --rm dotnet dotnet test
docker compose up web      # http://localhost:5000
```

## 基本ルール

- UI（Razor）にゲームロジックを書かない。ルール判定は Core に置く。
- Core / AI は `Random` を直接 `new` せず、時刻・I/O・ブラウザ API にも依存しない（決定的に保つ）。
- カード効果は、コードで実装した効果部品（`EffectAction`）の組み合わせとしてデータ（`CardEffect`）で定義する。カード専用の効果クラスは作らない。
- 実装したら `dotnet test` を通してから完了とする。Core / AI には異常系・境界値のテストを書く。
- 新しい NuGet パッケージの追加は、配布サイズに影響するため事前に確認する。
