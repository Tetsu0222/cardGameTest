using CardGame.AI.Strategies;
using CardGame.Core.Cards;
using CardGame.Core.Duels;
using CardGame.Core.Effects;
using CardGame.Core.Players;
using CardGame.Core.Summons;
using CardGame.Core.Turns;
using Microsoft.Extensions.Options;

namespace CardGame.Web.Features.Duels;

/// <summary>
/// 対戦画面の状態と操作。ルールの判定はすべて Core の <see cref="Duel"/> に任せ、
/// ここは「画面操作 → Core 呼び出し」と「CPU のターン実行」の橋渡しだけを行う。
/// </summary>
/// <remarks>
/// 複数の手順が必要な操作は、途中の状態を持つ。
/// <list type="bullet">
/// <item>召喚・セット: 手札のボタン → <see cref="PendingPlacement"/> →(アドバンス召喚なら <see cref="PendingTributes"/> を選ぶ →)マスを選ぶ</item>
/// <item>対象を取る魔法: 発動 → <see cref="PendingSpell"/> → 対象のモンスターを選ぶ</item>
/// <item>攻撃: 自分のモンスター → <see cref="SelectedAttacker"/> → 相手のモンスター(またはダイレクトアタック)</item>
/// </list>
/// CPU のターンは1手ずつ「何をするか(実況・強調表示)→ 結果」の順に間を置いて再生し、途中経過を <see cref="OnChange"/> で通知する。
/// 罠: あなたの攻撃宣言・召喚には CPU がその場で応じ、CPU の攻撃宣言・召喚には、あなたが発動するか選ぶまで再生を止めて待つ。
/// </remarks>
public sealed class DuelViewModel
{
    public const string HumanName = "あなた";
    public const string CpuName = "CPU";

    // CPU の1ターンは有限の手数で終わるが、画面が止まらないよう念のため上限を設ける
    private const int MaxCpuStepsPerTurn = 200;

    private readonly GameRulesOptions _rules;
    private readonly DuelViewOptions _viewOptions;
    private readonly Random _random;
    private readonly SimpleCpuStrategy _cpu;
    private readonly HashSet<FieldMonster> _highlightedMonsters = [];
    private readonly List<FieldMonster> _pendingTributes = [];
    private readonly Dictionary<Player, int> _lifeChanges = [];
    private bool _skipCpuPlayback;
    private TaskCompletionSource? _humanResponse;

    public DuelViewModel(
        IOptions<GameRulesOptions> rules,
        IOptions<DuelViewOptions> viewOptions,
        Random random,
        SimpleCpuStrategy cpu)
    {
        _rules = rules.Value;
        _viewOptions = viewOptions.Value;
        _random = random;
        _cpu = cpu;
        Duel = CreateDuel();
        AdvanceHumanToMain1();
        Commentary = new Commentary("あなたの先攻です");
    }

    /// <summary>
    /// 画面の再描画が必要になったときの通知。CPU のターンの再生中など、
    /// ボタン操作の処理が終わる前に途中経過を表示したいときに使う。
    /// </summary>
    public event Action? OnChange;

    public Duel Duel { get; private set; }

    /// <summary>CPU のターンを再生中か。再生中はプレイヤーの操作を受け付けない。</summary>
    public bool IsCpuPlaying { get; private set; }

    /// <summary>盤面中央に出す実況。</summary>
    public Commentary? Commentary { get; private set; }

    /// <summary>実況中の行動に関わるモンスター(攻撃するモンスターと攻撃対象、魔法の対象)。</summary>
    public IReadOnlyCollection<FieldMonster> HighlightedMonsters => _highlightedMonsters;
    public Player Human => Duel.FirstPlayer;
    public Player Cpu => Duel.SecondPlayer;

    /// <summary>攻撃宣言のために選択中の自分のモンスター。</summary>
    public FieldMonster? SelectedAttacker { get; private set; }

    /// <summary>召喚・セットを選んだ後、リリースするモンスターや置くマスを選んでいる途中の操作。</summary>
    public HandAction? PendingPlacement { get; private set; }

    /// <summary>アドバンス召喚でリリースするモンスターとして選んだもの。</summary>
    public IReadOnlyCollection<FieldMonster> PendingTributes => _pendingTributes;

    /// <summary>アドバンス召喚で、リリースするモンスターをまだ選んでいる途中か。</summary>
    public bool IsChoosingTributes =>
        PendingPlacement is { Kind: HandActionKind.Summon or HandActionKind.SetMonster }
        && _pendingTributes.Count < RequiredTributeCount;

    /// <summary>発動を選んだ後、対象を選んでいる途中の魔法。</summary>
    public PendingSpellActivation? PendingSpell { get; private set; }

    /// <summary>直前の操作が拒否された理由。</summary>
    public string? ErrorMessage { get; private set; }

    public bool IsHumanTurn => !IsCpuPlaying && !Duel.IsFinished && Duel.CurrentPlayer == Human;

    /// <summary>直前の行動によるライフの増減(減少は負の値)。変化がなければ null。</summary>
    public int? LifeChangeOf(Player player) => _lifeChanges.TryGetValue(player, out var change) ? change : null;

    public bool HasPendingOperation => PendingPlacement is not null || PendingSpell is not null;
    public bool CanGoToBattle => IsHumanTurn && Duel.CurrentPhase == Phase.Main1 && Duel.TurnCount > 1;
    public bool CanGoToMain2 => IsHumanTurn && Duel.CurrentPhase == Phase.Battle;
    public bool CanDirectAttack => SelectedAttacker is not null && Duel.CanAttack(SelectedAttacker, target: null).IsSuccess;

    /// <summary>選択中として強調表示する手札の位置。</summary>
    public int? SelectedHandIndex => PendingPlacement?.HandIndex ?? PendingSpell?.HandIndex;

    /// <summary>次に何をすればよいかの案内。</summary>
    public string Hint
    {
        get
        {
            if (!IsHumanTurn) return "";
            if (PendingSpell is not null) return $"「{PendingSpell.Card.Name}」の対象にするモンスターを選んでください。";
            if (PendingPlacement is { Kind: HandActionKind.SetSpellTrap }) return "セットするマスを選んでください。";
            if (IsChoosingTributes) return $"リリースするモンスターを選んでください(あと{RequiredTributeCount - _pendingTributes.Count}体)。";
            if (PendingPlacement is not null && _pendingTributes.Count > 0) return "モンスターを出すマスを選んでください(リリースするモンスターのマスも選べます)。";
            if (PendingPlacement is not null) return "モンスターを出すマスを選んでください。";
            if (Duel.CurrentPhase == Phase.Battle)
            {
                return SelectedAttacker is null ? "攻撃するモンスターを選んでください。" : "攻撃対象を選んでください。";
            }

            return Duel.CurrentPhase is Phase.Main1 or Phase.Main2 ? "カードを召喚・発動・セットしたり、モンスターの表示形式を変更したりできます。" : "";
        }
    }

    public void StartNewDuel()
    {
        if (IsCpuPlaying) return;
        Duel = CreateDuel();
        ClearSelection();
        ErrorMessage = null;
        _lifeChanges.Clear();
        _highlightedMonsters.Clear();
        AdvanceHumanToMain1();
        Commentary = new Commentary("あなたの先攻です");
    }

    // ---- 手札の操作 ----

    public bool CanDoHandAction(HandAction action) => IsHumanTurn && CheckHandAction(action).IsSuccess;

    /// <summary>
    /// 手札の操作を選ぶ。召喚・セットはマスの選択待ちに、対象を取る魔法は対象の選択待ちになる。
    /// 対象を取らない魔法はすぐに発動する。同じ操作をもう一度選ぶと取り消す。
    /// </summary>
    public void BeginHandAction(HandAction action)
    {
        if (!CanDoHandAction(action)) return;

        var wasSelected = PendingPlacement == action || (PendingSpell?.HandIndex == action.HandIndex && action.Kind == HandActionKind.ActivateSpell);
        ClearSelection();
        ErrorMessage = null;
        if (wasSelected) return;

        if (action.Kind != HandActionKind.ActivateSpell)
        {
            PendingPlacement = action;
            return;
        }

        var card = Human.Hand[action.HandIndex];
        if (card.Effect?.TargetSide is null)
        {
            Perform(() => Duel.ActivateSpellFromHand(action.HandIndex, target: null));
            return;
        }

        PendingSpell = new PendingSpellActivation(card, action.HandIndex, SetCard: null);
    }

    // ---- マスの選択 ----

    public bool CanPlaceInMonsterSlot(int slotIndex) =>
        IsHumanTurn && !IsChoosingTributes
        && PendingPlacement is { Kind: HandActionKind.Summon or HandActionKind.SetMonster } placement
        && Duel.CanSummon(placement.HandIndex, ToPosition(placement.Kind), slotIndex, [.. _pendingTributes]).IsSuccess;

    /// <summary>選んだマスに召喚する。CPU が罠で応じる場合は、その処理まで行う。</summary>
    public async Task PlaceInMonsterSlotAsync(int slotIndex)
    {
        if (!CanPlaceInMonsterSlot(slotIndex)) return;
        if (PendingPlacement is not { } placement) return;

        IReadOnlyList<FieldMonster> tributes = [.. _pendingTributes];
        ClearSelection();
        Perform(() => Duel.Summon(placement.HandIndex, ToPosition(placement.Kind), slotIndex, tributes));
        await LetCpuRespondAsync();
    }

    public bool CanPlaceInSpellTrapSlot(int slotIndex) =>
        IsHumanTurn && PendingPlacement is { Kind: HandActionKind.SetSpellTrap } placement
        && Duel.CanSetSpellTrap(placement.HandIndex, slotIndex).IsSuccess;

    public void PlaceInSpellTrapSlot(int slotIndex)
    {
        if (PendingPlacement is not { Kind: HandActionKind.SetSpellTrap } placement) return;
        PendingPlacement = null;
        Perform(() => Duel.SetSpellTrap(placement.HandIndex, slotIndex));
    }

    // ---- セットしたカード ----

    /// <summary>セットしたカードをクリックして発動できるか。自分のターンは魔法、CPU の行動に応じるときは罠。</summary>
    public bool CanActivateSetCard(SpellTrapCard setCard) =>
        IsAwaitingHumanResponse
            ? Duel.CanActivateTrap(setCard).IsSuccess
            : IsHumanTurn && !HasPendingOperation && Duel.CanActivateSetSpell(setCard).IsSuccess;

    /// <summary>セットしたカードを発動する。対象を取る魔法なら対象の選択待ちになる。</summary>
    public void ActivateSetCard(SpellTrapCard setCard)
    {
        if (!CanActivateSetCard(setCard)) return;
        if (IsAwaitingHumanResponse)
        {
            RespondWithTrap(setCard);
            return;
        }

        ClearSelection();
        ErrorMessage = null;

        if (setCard.Card.Effect?.TargetSide is null)
        {
            Perform(() => Duel.ActivateSetSpell(setCard, target: null));
            return;
        }

        PendingSpell = new PendingSpellActivation(setCard.Card, HandIndex: null, setCard);
    }

    // ---- モンスターのクリック(対象選択・攻撃) ----

    /// <summary>
    /// モンスターをクリックできるか。リリース選択中ならリリースできるもの、リリース後のマス選択中ならリリースするもの(そのマスに出す)、
    /// 対象選択中なら対象にできるもの、それ以外は攻撃の選択に使えるもの。
    /// </summary>
    public bool IsMonsterClickable(FieldMonster monster)
    {
        if (!IsHumanTurn) return false;

        if (PendingPlacement is { Kind: HandActionKind.Summon or HandActionKind.SetMonster })
        {
            return IsChoosingTributes
                ? Human.MonsterZone.Contains(monster) && !_pendingTributes.Contains(monster)
                : _pendingTributes.Contains(monster) && CanPlaceInMonsterSlot(Human.FindMonsterSlot(monster));
        }

        if (PendingPlacement is not null) return false;
        if (PendingSpell is not null) return CheckSpellTarget(PendingSpell, monster).IsSuccess;
        if (Human.MonsterZone.Contains(monster)) return CanSelectAttacker(monster);
        return SelectedAttacker is not null && Duel.CanAttack(SelectedAttacker, monster).IsSuccess;
    }

    public async Task ClickMonsterAsync(FieldMonster monster)
    {
        if (!IsMonsterClickable(monster)) return;

        if (IsChoosingTributes)
        {
            _pendingTributes.Add(monster);
        }
        else if (PendingPlacement is not null)
        {
            // リリースするモンスターをクリックしたら、そのマスに出す
            await PlaceInMonsterSlotAsync(Human.FindMonsterSlot(monster));
        }
        else if (PendingSpell is not null)
        {
            ActivatePendingSpell(PendingSpell, monster);
        }
        else if (Human.MonsterZone.Contains(monster))
        {
            SelectedAttacker = SelectedAttacker == monster ? null : monster;
            ErrorMessage = null;
        }
        else
        {
            await AttackAsync(monster);
        }
    }

    public Task DirectAttackAsync() => AttackAsync(target: null);

    // ---- 表示形式 ----

    /// <summary>
    /// 自分のモンスターの下に出す、表示形式を変更するボタンの文言。今変更できなければ null。
    /// 召喚・発動などの操作を選んでいる途中は、誤操作を避けるため出さない。
    /// </summary>
    public string? PositionChangeLabelFor(FieldMonster monster)
    {
        if (!IsHumanTurn || HasPendingOperation || SelectedAttacker is not null) return null;
        if (!Duel.CanChangePosition(monster).IsSuccess) return null;

        return monster.Position switch
        {
            BattlePosition.FaceUpAttack => "守備表示に",
            BattlePosition.FaceUpDefense => "攻撃表示に",
            _ => "反転召喚",
        };
    }

    /// <summary>表示形式を変更する(裏側なら反転召喚)。反転召喚に CPU が罠で応じる場合は、その処理まで行う。</summary>
    public async Task ChangePositionAsync(FieldMonster monster)
    {
        if (PositionChangeLabelFor(monster) is null) return;

        ErrorMessage = null;
        Perform(() => Duel.ChangePosition(monster));
        await LetCpuRespondAsync();
    }

    /// <summary>選択途中の召喚・セット・発動・攻撃をやめる。</summary>
    public void CancelSelection() => ClearSelection();

    // ---- フェーズ ----

    /// <summary>メイン1 → バトル、バトル → メイン2 のように1つ先のフェーズへ進む。</summary>
    public void GoToNextPhase()
    {
        if (!IsHumanTurn) return;
        ClearSelection();
        Perform(() => Duel.AdvancePhase());
    }

    /// <summary>自分のターンを終え、CPU のターンを1手ずつ再生し、次の自分のメインフェーズ1まで進める。</summary>
    public async Task EndTurnAsync()
    {
        if (!IsHumanTurn) return;
        ClearSelection();
        ErrorMessage = null;
        _lifeChanges.Clear();

        while (!Duel.IsFinished && Duel.CurrentPlayer == Human)
        {
            Duel.AdvancePhase();
        }

        if (!Duel.IsFinished)
        {
            await PlayCpuTurnAsync();
        }

        AdvanceHumanToMain1();
        if (IsHumanTurn)
        {
            Commentary = new Commentary("あなたのターンです");
        }

        NotifyChanged();
    }

    /// <summary>CPU のターンの残りを待ち時間なしで進める。</summary>
    public void SkipCpuPlayback() => _skipCpuPlayback = true;

    // ---- CPU のターンの再生 ----

    private async Task PlayCpuTurnAsync()
    {
        IsCpuPlaying = true;
        _skipCpuPlayback = false;
        try
        {
            Commentary = new Commentary($"{CpuName}のターン");
            await ShowAsync();

            for (var step = 0; step < MaxCpuStepsPerTurn && !Duel.IsFinished && Duel.CurrentPlayer == Cpu; step++)
            {
                var action = _cpu.DecideNextAction(Duel);
                if (action is null) break;

                var commentary = CpuActionCommentary.Create(action, Cpu);
                if (commentary is null)
                {
                    // フェーズ移動など、見せる必要のない手は待たずに進める
                    SimpleCpuStrategy.Execute(Duel, action);
                    continue;
                }

                // 1) これから何をするかを見せる
                Commentary = commentary;
                _lifeChanges.Clear();
                Highlight(action);
                await ShowAsync();

                // 2) 実行する。攻撃宣言・召喚にあなたが罠で応じられるなら、選ぶまで待つ
                Perform(() => SimpleCpuStrategy.Execute(Duel, action));
                ErrorMessage = null; // CPU の手が拒否されても、プレイヤーへのエラーとしては出さない
                if (Duel.PendingResponse?.Responder == Human)
                {
                    await WaitForHumanResponseAsync();
                }

                // 3) 結果(破壊・ライフの増減)を見せる
                await ShowAsync();
                _highlightedMonsters.Clear();
            }
        }
        finally
        {
            IsCpuPlaying = false;
            _highlightedMonsters.Clear();
        }
    }

    private void Highlight(CpuAction action)
    {
        _highlightedMonsters.Clear();
        switch (action)
        {
            case CpuAttackAction attack:
                _highlightedMonsters.Add(attack.Attacker);
                if (attack.Target is not null) _highlightedMonsters.Add(attack.Target);
                break;

            case CpuActivateSpellAction { Target: { } target }:
                _highlightedMonsters.Add(target);
                break;

            case CpuSummonAction { Tributes: { } tributes }:
                _highlightedMonsters.UnionWith(tributes);
                break;

            case CpuChangePositionAction change:
                _highlightedMonsters.Add(change.Monster);
                break;

            case CpuActivateTrapAction when Duel.PendingResponse is { } window:
                _highlightedMonsters.Add(window.TriggerMonster);
                break;
        }
    }

    // ---- 罠の応答 ----

    /// <summary>CPU の攻撃宣言・召喚に対して、あなたが罠を発動するかを選んでいる途中か。</summary>
    public bool IsAwaitingHumanResponse => _humanResponse is not null;

    /// <summary>今発動できるあなたの罠。</summary>
    public IReadOnlyList<SpellTrapCard> RespondableTraps => IsAwaitingHumanResponse ? Duel.FindActivatableTraps() : [];

    /// <summary>罠を発動するかを尋ねる文言。尋ねていなければ null。</summary>
    public string? ResponsePrompt => IsAwaitingHumanResponse && Duel.PendingResponse is { } window
        ? window.Trigger == TrapTrigger.OpponentAttackDeclared
            ? $"{CpuName}の「{window.TriggerMonster.Card.Name}」の攻撃宣言に対して、罠を発動しますか?"
            : $"{CpuName}の「{window.TriggerMonster.Card.Name}」の召喚に対して、罠を発動しますか?"
        : null;

    /// <summary>罠を発動する。発動が拒否された場合は、選び直せるよう待ち続ける。</summary>
    public void RespondWithTrap(SpellTrapCard trap)
    {
        if (_humanResponse is not { } response) return;

        Commentary = new Commentary($"{HumanName}は罠「{trap.Card.Name}」を発動", trap.Card);
        Perform(() => Duel.ActivateTrap(trap, target: null));
        if (Duel.PendingResponse is null)
        {
            response.TrySetResult();
        }
    }

    /// <summary>罠を発動せずに見送る。</summary>
    public void PassResponse()
    {
        if (_humanResponse is not { } response) return;

        Perform(() => Duel.PassResponse());
        response.TrySetResult();
    }

    private async Task WaitForHumanResponseAsync()
    {
        _humanResponse = new TaskCompletionSource();
        NotifyChanged();
        try
        {
            await _humanResponse.Task;
        }
        finally
        {
            _humanResponse = null;
        }
    }

    /// <summary>あなたの攻撃宣言・召喚に対して、CPU が罠で応じるかを決めて実行する。</summary>
    private async Task LetCpuRespondAsync()
    {
        if (Duel.PendingResponse?.Responder != Cpu) return;

        IsCpuPlaying = true;
        _skipCpuPlayback = false;
        try
        {
            var action = _cpu.DecideNextAction(Duel);
            if (action is null) return;

            if (CpuActionCommentary.Create(action, Cpu) is { } commentary)
            {
                Commentary = commentary;
                _lifeChanges.Clear();
                Highlight(action);
                await ShowAsync();
            }

            Perform(() => SimpleCpuStrategy.Execute(Duel, action));
            ErrorMessage = null;
        }
        finally
        {
            IsCpuPlaying = false;
            _highlightedMonsters.Clear();
            NotifyChanged();
        }
    }

    /// <summary>画面に反映し、スキップされていなければ少し待つ。</summary>
    private async Task ShowAsync()
    {
        NotifyChanged();
        if (!_skipCpuPlayback)
        {
            await Task.Delay(_viewOptions.CpuActionDelayMilliseconds);
        }
    }

    private void NotifyChanged() => OnChange?.Invoke();

    // ---- 内部処理 ----

    private ActionResult CheckHandAction(HandAction action) => action.Kind switch
    {
        HandActionKind.Summon or HandActionKind.SetMonster => Duel.CanSummon(action.HandIndex, ToPosition(action.Kind)),
        HandActionKind.ActivateSpell => Duel.CanActivateSpellFromHand(action.HandIndex),
        HandActionKind.SetSpellTrap => Duel.CanSetSpellTrap(action.HandIndex),
        _ => throw new ArgumentOutOfRangeException(nameof(action), action.Kind, "Unknown hand action."),
    };

    /// <summary>選択中の召喚に必要なリリースの数。召喚を選んでいなければ 0。</summary>
    private int RequiredTributeCount =>
        PendingPlacement is { Kind: HandActionKind.Summon or HandActionKind.SetMonster } placement
        && placement.HandIndex < Human.Hand.Count
            ? CountRequiredTributes.Execute(Human.Hand[placement.HandIndex])
            : 0;

    private static BattlePosition ToPosition(HandActionKind kind) =>
        kind == HandActionKind.SetMonster ? BattlePosition.FaceDownDefense : BattlePosition.FaceUpAttack;

    private bool CanSelectAttacker(FieldMonster monster) =>
        Duel.CanAttack(monster, target: null).IsSuccess
        || Cpu.MonsterZone.Any(target => Duel.CanAttack(monster, target).IsSuccess);

    private ActionResult CheckSpellTarget(PendingSpellActivation pending, FieldMonster target) =>
        pending.SetCard is not null
            ? Duel.CanActivateSetSpell(pending.SetCard, target)
            : Duel.CanActivateSpellFromHand(pending.HandIndex ?? -1, target);

    private void ActivatePendingSpell(PendingSpellActivation pending, FieldMonster target)
    {
        PendingSpell = null;
        Perform(() => pending.SetCard is not null
            ? Duel.ActivateSetSpell(pending.SetCard, target)
            : Duel.ActivateSpellFromHand(pending.HandIndex ?? -1, target));
    }

    private async Task AttackAsync(FieldMonster? target)
    {
        if (SelectedAttacker is not { } attacker) return;
        SelectedAttacker = null;
        Perform(() => Duel.Attack(attacker, target));
        await LetCpuRespondAsync();
    }

    private Duel CreateDuel()
    {
        var human = new Player(HumanName, ShuffleDeck.Execute(SampleDeck.Create(), _random), _rules);
        var cpu = new Player(CpuName, ShuffleDeck.Execute(SampleDeck.Create(), _random), _rules);
        return new Duel(human, cpu, _rules);
    }

    /// <summary>ドロー・スタンバイでは今できる操作がないため、自動でメインフェーズ1まで進める。</summary>
    private void AdvanceHumanToMain1()
    {
        while (IsHumanTurn && Duel.CurrentPhase is (Phase.Draw or Phase.Standby))
        {
            Duel.AdvancePhase();
        }
    }

    private void ClearSelection()
    {
        SelectedAttacker = null;
        PendingPlacement = null;
        _pendingTributes.Clear();
        PendingSpell = null;
    }

    /// <summary>操作を実行し、ライフの増減と、拒否された場合の理由を記録する。</summary>
    private void Perform(Func<ActionResult> action)
    {
        (Player Player, int LifePoints)[] before = [(Human, Human.LifePoints), (Cpu, Cpu.LifePoints)];
        _lifeChanges.Clear();

        var result = action();

        foreach (var (player, lifePoints) in before)
        {
            var change = player.LifePoints - lifePoints;
            if (change != 0) _lifeChanges[player] = change;
        }

        ErrorMessage = result.IsSuccess ? null : ActionErrorMessages.ToMessage(result.Error);
    }
}
