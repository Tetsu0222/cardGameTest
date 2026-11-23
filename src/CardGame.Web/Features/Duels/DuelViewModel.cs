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

    /// <summary>召喚・セットを選んだ後、出すマスを選んでいる途中の操作。</summary>
    public SummonRequest? PendingSummon { get; private set; }

    /// <summary>直前の操作が拒否された理由。</summary>
    public string? ErrorMessage { get; private set; }

    public bool IsHumanTurn => !Duel.IsFinished && Duel.CurrentPlayer == Human;
    public bool CanGoToBattle => IsHumanTurn && Duel.CurrentPhase == Phase.Main1 && Duel.TurnCount > 1;
    public bool CanGoToMain2 => IsHumanTurn && Duel.CurrentPhase == Phase.Battle;
    public bool CanDirectAttack => SelectedAttacker is not null && Duel.CanAttack(SelectedAttacker, target: null).IsSuccess;

    /// <summary>次に何をすればよいかの案内。</summary>
    public string Hint => (IsHumanTurn, Duel.CurrentPhase, SelectedAttacker) switch
    {
        (false, _, _) => "",
        _ when PendingSummon is not null => "モンスターを出すマスを選んでください。",
        (true, Phase.Battle, null) => "攻撃するモンスターを選んでください。",
        (true, Phase.Battle, _) => "攻撃対象を選んでください。",
        (true, Phase.Main1 or Phase.Main2, _) when !Duel.NormalSummonUsed => "モンスターを召喚またはセットできます。",
        _ => "",
    };

    public void StartNewDuel()
    {
        Duel = CreateDuel();
        ClearSelection();
        ErrorMessage = null;
        AdvanceHumanToMain1();
    }

    public bool CanSummon(int handIndex, BattlePosition position) =>
        IsHumanTurn && Duel.CanSummon(handIndex, position).IsSuccess;

    /// <summary>召喚・セットを選ぶ。続けて <see cref="SummonToSlot"/> でマスを選ぶ。同じ操作をもう一度選ぶと取り消す。</summary>
    public void BeginSummon(SummonRequest request)
    {
        if (!CanSummon(request.HandIndex, request.Position)) return;
        PendingSummon = PendingSummon == request ? null : request;
        SelectedAttacker = null;
        ErrorMessage = null;
    }

    public void CancelSummon() => PendingSummon = null;

    public bool CanSummonToSlot(int slotIndex) =>
        PendingSummon is not null
        && IsHumanTurn
        && Duel.CanSummon(PendingSummon.HandIndex, PendingSummon.Position, slotIndex).IsSuccess;

    public void SummonToSlot(int slotIndex)
    {
        if (PendingSummon is null) return;
        var request = PendingSummon;
        PendingSummon = null;
        Apply(Duel.Summon(request.HandIndex, request.Position, slotIndex));
    }

    /// <summary>そのモンスターで攻撃できる対象が1つでもあれば選択可能。</summary>
    public bool CanSelectAttacker(FieldMonster monster) =>
        IsHumanTurn
        && (Duel.CanAttack(monster, target: null).IsSuccess
            || Cpu.MonsterZone.Any(target => Duel.CanAttack(monster, target).IsSuccess));

    public void SelectAttacker(FieldMonster monster)
    {
        if (!CanSelectAttacker(monster)) return;
        SelectedAttacker = SelectedAttacker == monster ? null : monster;
        PendingSummon = null;
        ErrorMessage = null;
    }

    public bool CanAttackTarget(FieldMonster target) =>
        SelectedAttacker is not null && Duel.CanAttack(SelectedAttacker, target).IsSuccess;

    /// <param name="target">攻撃対象。null はダイレクトアタック。</param>
    public void AttackTarget(FieldMonster? target)
    {
        if (SelectedAttacker is null) return;
        var result = Duel.Attack(SelectedAttacker, target);
        SelectedAttacker = null;
        Apply(result);
    }

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
        PendingSummon = null;
    }

    private void Apply(ActionResult result)
    {
        ErrorMessage = result.IsSuccess ? null : ActionErrorMessages.ToMessage(result.Error);
    }
}
