# コーディング規約

C# / .NET 10 / Blazor WebAssembly（スタンドアロン）を前提に、変更に強く文脈が閉じた設計を標準とする。
大規模開発および AI 駆動開発でも読み違えにくい構成を重視する。

---

## 1. 共通原則

### 1-1. UI にゲームロジックを書かない

Razor コンポーネントの責務は「状態の表示 → ユーザー操作の受け取り → ロジック層の呼び出し」のみ。
ルール判定・ダメージ計算・フェーズ遷移などは `CardGame.Core` に置く。

| プロジェクト | 責務 |
|---|---|
| CardGame.Web | 画面表示・入力受付・DI 登録・ブラウザ API（JS 連携）|
| CardGame.Core | ゲームルール・ドメインモデル（UI / ブラウザ非依存）|
| CardGame.AI | 対戦 AI の思考ロジック（Core にのみ依存）|
| CardGame.Tests | Core / AI のユニットテスト |

**依存方向：** `Web → AI`、`Web → Core`、`AI → Core`、`Tests → Core / AI`（逆方向の参照は禁止）
（Web は表示のため Core のモデルを直接参照してよい。Core は何にも依存しない。）

- `Core` / `AI` は `Microsoft.AspNetCore.*`、`HttpClient`、`IJSRuntime` に依存しない。
  純粋な C# として `dotnet test` だけで検証できる状態を保つ。
- コンポーネントの `@code` が数行を超えるロジックを持ち始めたら Core へ移す。

### 1-2. Core は副作用を持たず決定的にする

- 乱数（シャッフル等）は `Random` を直接 `new` せず、`Random` / `IRandom` を引数または
  コンストラクタで受け取る。テストでシード固定できるようにする。
- 時刻・I/O・ブラウザ API を Core から直接呼ばない。必要ならインターフェースを Core に定義し、
  実装を Web 側に置く（例：`IGameStorage` を Core に、`LocalStorage` 実装を Web に）。

### 1-3. 不変にできるものは不変にする

- カード定義などの静的データは `record`（`sealed record`）で表現する。
- 外部から変更させないプロパティは `private set` / `init` / `IReadOnlyList<T>` で公開する。
  （例：`List<CardData>` をそのまま公開せず、変更は専用メソッド経由にする）
- `Nullable` は有効（`<Nullable>enable</Nullable>`）。`!`（null 許容抑制）は原則使わない。

---

## 2. プロジェクト・フォルダ構成

### 2-1. 構成の基本

プロジェクトで大分類し、その中は**機能（ドメイン）単位**でフォルダを切る。
これ以上深くネストしない。階層が深いほど AI の文脈理解コストが上がる。

```
/CardGame.slnx
/docker-compose.yml
/docs
/src
├── CardGame.Core
│   ├── Cards            (CardData, CardCatalog, CardCatalogJson ... カード定義と読み込み)
│   ├── Decks            (DeckList, ValidateDeck, IDeckRepository ... デッキの構成・検証・保存先)
│   ├── Summons          (召喚のルール)
│   ├── Players          (Player ...)
│   ├── Turns            (Phase ...)
│   ├── Duels            (Duel, GameRulesOptions ... 盤面全体の状態と進行)
│   ├── Battle           (戦闘・ダメージ計算)
│   ├── Effects (補足5)
│   └── Shared (補足1)
├── CardGame.AI
│   └── Strategies       (対戦 AI の戦略)
└── CardGame.Web
    ├── Program.cs
    ├── App.razor
    ├── _Imports.razor
    ├── Layout           (MainLayout, NavMenu)
    ├── Features (補足2)
    │   ├── Duels
    │   │   ├── Pages        (DuelPage.razor)
    │   │   ├── Components   (HandView.razor, FieldView.razor)
    │   │   └── DuelViewModel.cs
    │   ├── DeckEditor
    │   │   ├── Pages
    │   │   └── Components
    │   └── ...etc
    ├── Shared               (機能をまたぐ部品・状態: CardView, GameData ...)
    ├── Services (補足3)
    └── wwwroot
        ├── appsettings.json
        ├── css
        ├── data
        │   └── cards.json   (カードの定義と初期デッキ。テストでも読み込んで検証する)
        ├── js           (JS 連携の ES モジュール。4-4)
        └── images
            └── cards
                └── {cardId}.png (補足4)
/tests
└── CardGame.Tests
    ├── Cards
    ├── Players
    └── ...(Core / AI のフォルダ構成と同じ)
```

**補足**
- 1.`Shared` に入れる条件は 2-5 を参照。
- 2.Web の画面は `Pages/` に平置きせず、機能単位の `Features/{機能名}/` にまとめる。
    - ページ・専用コンポーネント・ViewModel が同じフォルダに閉じるため、変更範囲が局所化する。
    - 機能をまたいで使う汎用コンポーネントのみ `Web/Shared/` に置く。
- 3.`Services` にはブラウザ依存の実装（LocalStorage・HttpClient ラッパー・JS 連携）のみ置く。ゲームルールは置かない。
- 4.カード画像は ID をファイル名にして `wwwroot/images/cards/{cardId}.png` に置く。1カード＝1ファイルで管理する。
- 5.`Effects` にはカード効果の仕組みを置く。効果は**部品の組み合わせ**で表す（将来のカード作成機能のため）。
    - 効果部品（`DrawCardsAction`・`DealDamageAction` など、`EffectAction` の派生 record）の**処理はコードで実装する**。
    - カードごとの効果は「どの部品をどの数値で、どの順に並べるか」を `CardEffect` としてデータで定義する。カード専用のクラスは作らない。
    - 部品を追加するときは、`ResolveCardEffect`（処理）・`ValidateCardEffect`（定義の検証）・`EffectAction` の `JsonDerivedType`（JSON での種類名）・Web の `CardEffectText`（説明文）をセットで更新する。
    - カードの定義と初期デッキは `wwwroot/data/cards.json` に置く（5-4）。JSON の変換は Core の `CardCatalogJson` で行い、ファイルの取得だけを Web の `Services` で行う。

### 2-2. 名前空間・ファイル配置

- 名前空間はフォルダ構成と一致させる（例：`CardGame.Core.Cards`）。
- 1ファイル = 1つの公開型。ファイル名は型名と一致させる。
- ファイルスコープ名前空間（`namespace CardGame.Core;`）を使う。
- 例外として、数行程度の小さな `enum` は、それを主に使う型と同じファイルに置いてよい（例：`CardKind` を `CardData.cs` に置く）。`enum` 以外は例外なし。

### 2-3. Razor コンポーネントの構成

- 1コンポーネント = 1 `.razor`。ロジックが増えたら code-behind（`Xxx.razor.cs`）に `partial class` として分離する。
- コンポーネント固有のスタイルは CSS isolation（`Xxx.razor.css`）を使う。
- ファイル名は PascalCase。ページは `{名詞}Page.razor`（`DuelPage.razor`）、部品は役割が分かる名前（`HandView.razor`）。
- Web の機能フォルダ名・ページ名は Core の型名と同じにしない（例：`Features/Duel` だと名前空間 `...Features.Duel` が Core の `Duel` 型と衝突する）。機能フォルダは複数形（`Duels`）にする。
- `@page` のルートは小文字ケバブケース（`/deck-editor`）にする。

### 2-4. ユースケースの粒度基準

ロジックが多い機能は「動詞 + 名詞」のユースケース単位でクラスを分ける。

```
/CardGame.Core/Battle
    ResolveBattle.cs          (戦闘解決)
    CalculateDamage.cs        (ダメージ計算)
/CardGame.Core/Turns
    AdvancePhase.cs
    DrawCard.cs
```

**ルール**
- 1クラス = 1責務。巨大な `GameManager` / `Utils` を作らない。
- クラス名・ファイル名にドメイン名を含める（冗長でもOK）。AI が検索しやすくなる。

### 2-5. Shared の扱い

複数の機能で使う共通処理は `Shared/` にまとめる。

**Shared に入れる条件：**
- 2つ以上の機能で使う
- ドメインに依存しない

**Shared に入れてはいけないもの：**
- ドメイン固有のロジック
- 特定の機能固有のルール
- 特定の集約（Player / Duel 等）に依存する処理

**依存方向：** 各機能 → `Shared`（`Shared` から各機能への参照は禁止）

---

## 3. コーディングスタイル

### 3-1. 命名

| 対象 | 規則 | 例 |
|---|---|---|
| 型・メソッド・プロパティ・enum 値 | PascalCase | `CardData`, `TakeDamage` |
| ローカル変数・引数 | camelCase | `lifePoints` |
| private フィールド | `_camelCase` | `_random` |
| インターフェース | `I` + PascalCase | `IGameStorage` |
| 定数 | PascalCase | `InitialLifePoints` |
| 非同期メソッド | `Async` サフィックス | `LoadDeckAsync` |

### 3-2. 言語機能の使い方

- `var` は型が右辺から明らかな場合のみ。
- 単純な型は `record`、振る舞いを持つ型は `class`。継承を想定しない型は `sealed`。
- コレクション式（`[]`）、プライマリコンストラクタ、パターンマッチングなど最新の構文は、可読性が上がる場合に使う。
- マジックナンバーは定数化する。ゲームバランスに関わる値（初期ライフ等）は定数ではなく 5-2 の設定（`GameRulesOptions`）から受け取る。
- 例外は「起こり得ない状態」にのみ使う。ゲーム上の通常の失敗（デッキ切れ等）は戻り値で表す。

### 3-3. 非同期

- `async void` は禁止（Razor のイベントハンドラは `async Task`）。
- `.Result` / `.Wait()` は禁止。WebAssembly はシングルスレッドのためデッドロックや固まりの原因になる。
- 重い処理（AI の探索など）は UI を固めないよう、一定単位で `await Task.Yield()` を挟むか、処理量を制限する。

---

## 4. Blazor WebAssembly 固有の規約

### 4-1. 状態管理

- 画面をまたぐ状態は DI 登録したサービス（`AddScoped` / `AddSingleton`）で保持する。
  WebAssembly では Scoped と Singleton は実質同じ寿命だが、意図が伝わるよう「アプリ全体で1つ」なら `Singleton`、そうでなければ `Scoped` を使う。
- ページ内だけの状態はコンポーネントのフィールドで持つ。
- 親子間は `[Parameter]` + `EventCallback` で受け渡し、子から親の状態を直接書き換えない。
- 状態を変更するサービスは `event Action? OnChange` 等で通知し、コンポーネント側で `StateHasChanged()` を呼ぶ。購読は `IDisposable` で必ず解除する。

### 4-2. コンポーネント

- `[Parameter]` は `public` プロパティのみ。必須のものは `[EditorRequired]` を付ける。
- `[Parameter]` に渡されたオブジェクトをコンポーネント内で変更しない。
- `@foreach` で描画する要素には `@key` を付ける（カード一覧など並び替えが起こるもの）。
- 描画のたびに走る処理（`OnParametersSet` や `@code` 内のプロパティ getter）で重い計算をしない。
- 初期化は `OnInitialized(Async)`、パラメータ依存の処理は `OnParametersSet(Async)` に書く。

### 4-3. DI

- サービスは `Program.cs` で登録し、コンポーネントでは `@inject`、クラスではコンストラクタ注入で受け取る。
- `Program.cs` の登録は機能ごとに拡張メソッド（`AddDuelServices()` 等）にまとめ、明示的に呼ぶ。自動スキャンによる登録は行わない。
    - 自動登録だと「どこで登録されているか」を推測する必要があり、文脈理解コストが上がる。

### 4-4. JavaScript 連携

- JS Interop は `Web/Services` の専用ラッパー経由に限定し、コンポーネントから `IJSRuntime` を直接呼ばない。
- JS 関数は `wwwroot/js/` に ES モジュールとして置き、`IJSObjectReference` で読み込む。グローバル関数を増やさない。
- JS 側へ渡す・戻す値はシリアライズ可能な単純型にする。

### 4-5. HTTP・外部データ

- `HttpClient` はコンポーネントで直接使わず、`Web/Services` のクライアントクラスにまとめる。
- API の URL・パスは設定（4-6）から取得し、コード中にハードコードしない。
- 通信失敗（タイムアウト・404・500）は呼び出し側でハンドリングし、ユーザーに分かる形で表示する。

### 4-6. パフォーマンス・配布サイズ

- 全コードがブラウザにダウンロードされるため、不要な NuGet パッケージは追加しない。
- リリースビルドでは IL トリミングと AOT の採用可否を検討する（AOT は実行速度が上がるがサイズが増える）。
- 大きな画像は事前に圧縮し、必要になるまで読み込まない（遅延ロード）。

---

## 5. 設定（config 駆動設計）

### 5-1. 設定の置き場所

- Blazor WASM の設定ファイルは **クライアントに全て公開される**。`wwwroot/appsettings.json` に **秘密情報（API キー・パスワード等）を絶対に置かない**。
- 環境別の値は `wwwroot/appsettings.{Environment}.json` に置く。
- 設定は `IConfiguration` を直接読まず、型付きクラスにバインドして `IOptions<T>` で受け取る。
- `Options` クラス（例：`GameRulesOptions`）は **依存のない素のクラスとして Core に置く**。`IOptions<T>` へのバインドは Web の `Program.cs` のみで行い、Core / AI は `Options` オブジェクトをコンストラクタ引数で受け取る。

```csharp
builder.Services.Configure<GameRulesOptions>(
    builder.Configuration.GetSection("GameRules"));
```

### 5-2. 設定化すべきもの

- 外部 API のエンドポイント
- ゲームバランスの定数・閾値（初期ライフ、初期手札枚数、デッキ枚数の上下限など）
- 機能フラグ（ON/OFF）
- AI の強さ・パラメータ

### 5-3. 条件付きで設定化してよいもの

- 分岐条件の判定値
- 画面表示の切り替え

### 5-4. 設定化すべきでないもの

- ゲームルールそのもの（ビジネスロジック）
- 複雑な計算
- カード効果の処理（効果部品の中身。部品の組み合わせと数値はデータで定義してよい。2-1 補足5）
- モデルの振る舞い

> カードデータ（名前・攻撃力など）は静的データとして `CardData` で定義する。
> 数が増える場合は JSON として `wwwroot/data/` に置いて読み込んでよい。効果は部品の組み合わせ（`CardEffect`）として記述し、部品にない処理が必要になったら部品をコードで追加する。

---

## 6. バリデーション

- ユーザー入力フォームは `EditForm` + `DataAnnotationsValidator` を使い、検証属性（`[Required]` / `[Range]` 等）を入力モデルに付ける。
- **ゲームルール上の検証**（デッキ枚数・同名カード枚数制限・召喚可否など）は Core に置き、Razor 側に書かない。結果は `ValidationResult` 等の戻り値で UI に返す。
- 検証に使う閾値は 5-2 の設定から取得する。

---

## 7. テスト規約

### 7-1. 基本方針

- テストは xUnit（`[Fact]` / `[Theory]`）で書く。Core と AI は必ずテスト対象にする。
- テストクラスは対象クラスと対応させる（`Player` → `PlayerTests`）。
- テストメソッド名は `対象_期待結果_条件`（例：`Draw_ReturnsFalse_WhenDeckEmpty`）。
- 1テスト = 1つの振る舞いを検証する。Arrange / Act / Assert を意識して書く。
- 乱数は固定シードまたはテスト用の `IRandom` で決定的にする。
- テスト内でマジック値を使い回さず、ヘルパー（`CreatePlayer(deckSize)` のようなファクトリ）にまとめる。
- 複数のテストクラスで使うヘルパー（盤面を組み立てる `DuelBuilder`、テスト用カードの `TestCards` など）は `tests/CardGame.Tests/TestSupport/` に置く。
- ルールの検証は、`DuelBuilder` で盤面を作り `Duel` の公開操作を通して確かめるシナリオテストを中心にする。純粋な計算（戦闘結果など）は単体テストで網羅する。

### 7-2. 異常系・境界値テストを書く

正常系だけでなく以下を必ずカバーする。

- デッキが空・手札が上限など境界の状態
- ライフポイントが 0 になる・負になる場合
- 不正な入力（null・範囲外の値）
- 外部 API が 500 / 404 を返す場合（`HttpClient` を使う機能）

### 7-3. UI のテスト

- Razor コンポーネントのテストが必要な場合は bUnit を使う（導入時は `tests/` に専用プロジェクトを追加する）。
- ロジックは Core に寄せているため、UI テストは「表示・イベント発火」の確認に絞る。

### 7-4. 実行方法

ローカルに .NET SDK がなくても Docker で実行できる。

```
docker compose run --rm dotnet dotnet build
docker compose run --rm dotnet dotnet test
docker compose up web      # http://localhost:5000
```

### 7-5. 新規設定値追加時のチェックリスト

- [ ] `wwwroot/appsettings.json` に既定値を追記
- [ ] 対応する `Options` クラスに型付きプロパティを追加
- [ ] 機密情報でないことを確認（クライアントに公開される）
- [ ] テストが `Options` を経由して値を参照していること（値のハードコードをしない）

---

## 8. 禁止事項まとめ

- Core / AI から Web・ブラウザ API への依存
- Razor コンポーネントへのゲームルールの記述
- `async void`、`.Result`、`.Wait()`
- `appsettings.json` への秘密情報の記載
- `IJSRuntime` / `HttpClient` のコンポーネントからの直接利用
- 自動スキャンによる DI 登録
- `Utils` / `Manager` / `Helper` といった責務の曖昧なクラス
