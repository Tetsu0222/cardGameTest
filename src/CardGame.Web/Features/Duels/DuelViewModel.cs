using CardGame.AI.Strategies;
using CardGame.Core.Cards;
using CardGame.Core.Duels;
using CardGame.Core.Players;
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
/// <item>召喚・セット: 手札のボタン → <see cref="PendingPlacement"/> → 空きマスを選ぶ</item>
/// <item>対象を取る魔法: 発動 → <see cref="PendingSpell"/> → 対象のモンスターを選ぶ</item>
/// <item>攻撃: 自分のモンスター → <see cref="SelectedAttacker"/> → 相手のモンスター(またはダイレクトアタック)</item>
/// </list>
/// </remarks>
public sealed class DuelViewModel
{
    public const string HumanName = "あなた";
    public const string CpuName = "CPU";

    private readonly GameRulesOptions _rules;
    private readonly Random _random;
    private readonly SimpleCpuStrategy _cpu;

    public DuelViewModel(IOptions<GameRulesOptions> rules, Random random, SimpleCpuStrategy cpu)
    {
        _rules = rules.Value;
        _random = random;
        _cpu = cpu;
        Duel = CreateDuel();
        AdvanceHumanToMain1();
    }

    public Duel Duel { get; private set; }
    public Player Human => Duel.FirstPlayer;
    public Player Cpu => Duel.SecondPlayer;

    /// <summary>攻撃宣言のために選択中の自分のモンスター。</summary>
    public FieldMonster? SelectedAttacker { get; private set; }

    /// <summary>召喚・セットを選んだ後、置くマスを選んでいる途中の操作。</summary>
    public HandAction? PendingPlacement { get; private set; }

    /// <summary>発動を選んだ後、対象を選んでいる途中の魔法。</summary>
    public PendingSpellActivation? PendingSpell { get; private set; }

    /// <summary>直前の操作が拒否された理由。</summary>
    public string? ErrorMessage { get; private set; }

    public bool IsHumanTurn => !Duel.IsFinished && Duel.CurrentPlayer == Human;
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
            if (PendingPlacement is not null) return "モンスターを出すマスを選んでください。";
            if (Duel.CurrentPhase == Phase.Battle)
            {
                return SelectedAttacker is null ? "攻撃するモンスターを選んでください。" : "攻撃対象を選んでください。";
            }

            return Duel.CurrentPhase is Phase.Main1 or Phase.Main2 ? "カードを召喚・発動・セットできます。" : "";
        }
    }

    public void StartNewDuel()
    {
        Duel = CreateDuel();
        ClearSelection();
        ErrorMessage = null;
        AdvanceHumanToMain1();
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
            Apply(Duel.ActivateSpellFromHand(action.HandIndex, target: null));
            return;
        }

        PendingSpell = new PendingSpellActivation(card, action.HandIndex, SetCard: null);
    }

    // ---- マスの選択 ----

    public bool CanPlaceInMonsterSlot(int slotIndex) =>
        IsHumanTurn && PendingPlacement is { Kind: HandActionKind.Summon or HandActionKind.SetMonster } placement
        && Duel.CanSummon(placement.HandIndex, ToPosition(placement.Kind), slotIndex).IsSuccess;

    public void PlaceInMonsterSlot(int slotIndex)
    {
        if (PendingPlacement is not { Kind: HandActionKind.Summon or HandActionKind.SetMonster } placement) return;
        PendingPlacement = null;
        Apply(Duel.Summon(placement.HandIndex, ToPosition(placement.Kind), slotIndex));
    }

    public bool CanPlaceInSpellTrapSlot(int slotIndex) =>
        IsHumanTurn && PendingPlacement is { Kind: HandActionKind.SetSpellTrap } placement
        && Duel.CanSetSpellTrap(placement.HandIndex, slotIndex).IsSuccess;

    public void PlaceInSpellTrapSlot(int slotIndex)
    {
        if (PendingPlacement is not { Kind: HandActionKind.SetSpellTrap } placement) return;
        PendingPlacement = null;
        Apply(Duel.SetSpellTrap(placement.HandIndex, slotIndex));
    }

    // ---- セットしたカード ----

    public bool CanActivateSetCard(SpellTrapCard setCard) =>
        IsHumanTurn && !HasPendingOperation && Duel.CanActivateSetSpell(setCard).IsSuccess;

    /// <summary>セットした魔法を発動する。対象を取る魔法なら対象の選択待ちになる。</summary>
    public void ActivateSetCard(SpellTrapCard setCard)
    {
        if (!CanActivateSetCard(setCard)) return;
        ClearSelection();
        ErrorMessage = null;

        if (setCard.Card.Effect?.TargetSide is null)
        {
            Apply(Duel.ActivateSetSpell(setCard, target: null));
            return;
        }

        PendingSpell = new PendingSpellActivation(setCard.Card, HandIndex: null, setCard);
    }

    // ---- モンスターのクリック(対象選択・攻撃) ----

    /// <summary>モンスターをクリックできるか。対象選択中なら対象にできるもの、それ以外は攻撃の選択に使えるもの。</summary>
    public bool IsMonsterClickable(FieldMonster monster)
    {
        if (!IsHumanTurn || PendingPlacement is not null) return false;
        if (PendingSpell is not null) return CheckSpellTarget(PendingSpell, monster).IsSuccess;
        if (Human.MonsterZone.Contains(monster)) return CanSelectAttacker(monster);
        return SelectedAttacker is not null && Duel.CanAttack(SelectedAttacker, monster).IsSuccess;
    }

    public void ClickMonster(FieldMonster monster)
    {
        if (!IsMonsterClickable(monster)) return;

        if (PendingSpell is not null)
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
            Attack(monster);
        }
    }

    public void DirectAttack() => Attack(target: null);

    /// <summary>選択途中の召喚・セット・発動・攻撃をやめる。</summary>
    public void CancelSelection() => ClearSelection();

    // ---- フェーズ ----

    /// <summary>メイン1 → バトル、バトル → メイン2 のように1つ先のフェーズへ進む。</summary>
    public void GoToNextPhase()
    {
        if (!IsHumanTurn) return;
        ClearSelection();
        Apply(Duel.AdvancePhase());
    }

    /// <summary>自分のターンを終え、CPU のターンを実行し、次の自分のメインフェーズ1まで進める。</summary>
    public void EndTurn()
    {
        if (!IsHumanTurn) return;
        ClearSelection();
        ErrorMessage = null;

        while (IsHumanTurn)
        {
            Duel.AdvancePhase();
        }

        if (!Duel.IsFinished)
        {
            _cpu.PlayTurn(Duel);
        }

        AdvanceHumanToMain1();
    }

    // ---- 内部処理 ----

    private ActionResult CheckHandAction(HandAction action) => action.Kind switch
    {
        HandActionKind.Summon or HandActionKind.SetMonster => Duel.CanSummon(action.HandIndex, ToPosition(action.Kind)),
        HandActionKind.ActivateSpell => Duel.CanActivateSpellFromHand(action.HandIndex),
        HandActionKind.SetSpellTrap => Duel.CanSetSpellTrap(action.HandIndex),
        _ => throw new ArgumentOutOfRangeException(nameof(action), action.Kind, "Unknown hand action."),
    };

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
        Apply(pending.SetCard is not null
            ? Duel.ActivateSetSpell(pending.SetCard, target)
            : Duel.ActivateSpellFromHand(pending.HandIndex ?? -1, target));
    }

    private void Attack(FieldMonster? target)
    {
        if (SelectedAttacker is null) return;
        var result = Duel.Attack(SelectedAttacker, target);
        SelectedAttacker = null;
        Apply(result);
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
        PendingSpell = null;
    }

    private void Apply(ActionResult result)
    {
        ErrorMessage = result.IsSuccess ? null : ActionErrorMessages.ToMessage(result.Error);
    }
}
