using UnityEngine;

// 히어로와 몬스터가 공유하는 전투 애니메이션 재생기입니다.
// 프리팹에는 Animator만 있으면 되고, 전투 View 계층은 이 클래스를 통해 공통 상태 이름만 요청합니다.
public sealed class CombatUnitAnimationPlayer
{
    private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed"); // 이동 애니메이션 배속 파라미터입니다.
    private static readonly int AttackSpeedHash = Animator.StringToHash("AttackSpeed"); // 공격 애니메이션 배속 파라미터입니다.

    private readonly Animator animator; // 실제 Unity Animator입니다.
    private readonly int idleStateHash; // 실제 Animator Controller에 존재하는 대기 상태 해시입니다.
    private readonly int moveStateHash; // 실제 Animator Controller에 존재하는 이동 상태 해시입니다.
    private readonly int attackStateHash; // 실제 Animator Controller에 존재하는 공격 상태 해시입니다.
    private readonly int hitStateHash; // 실제 Animator Controller에 존재하는 피격 상태 해시입니다.
    private readonly int dieStateHash; // 실제 Animator Controller에 존재하는 사망 상태 해시입니다.
    private readonly bool hasMoveSpeedParameter; // MoveSpeed 파라미터 지원 여부입니다.
    private readonly bool hasAttackSpeedParameter; // AttackSpeed 파라미터 지원 여부입니다.
    private readonly float defaultCrossFadeSeconds; // 반복 상태 전환에 사용할 기본 블렌딩 시간입니다.

    private CombatUnitAnimationState currentState = CombatUnitAnimationState.Idle; // 마지막으로 요청된 애니메이션 상태입니다.

    public CombatUnitAnimationState CurrentState => currentState; // 외부 확인용 현재 애니메이션 상태입니다.

    // Animator와 공통 전환 시간을 받아 재생기를 구성합니다.
    public CombatUnitAnimationPlayer(Animator animator, float defaultCrossFadeSeconds = 0.06f)
    {
        this.animator = animator;
        this.defaultCrossFadeSeconds = defaultCrossFadeSeconds;
        idleStateHash = ResolveStateHash("Idle");
        moveStateHash = ResolveStateHash("Move", "Run", "Walk");
        attackStateHash = ResolveStateHash("Attack", "Double Attack", "Jump Attack");
        hitStateHash = ResolveStateHash("Hit", "Stun");
        dieStateHash = ResolveStateHash("Die", "Dead1", "Dead2", "Dead3", "Defeat");
        hasMoveSpeedParameter = HasAnimatorParameter(MoveSpeedHash);
        hasAttackSpeedParameter = HasAnimatorParameter(AttackSpeedHash);
    }

    // 풀에서 다시 꺼낼 때 이전 트리거와 재생 시간을 초기화합니다.
    public void ResetPlayback()
    {
        if (animator == null)
            return;

        animator.Rebind();
        animator.Update(0f);
        currentState = CombatUnitAnimationState.Idle;
        PlayIdle(true);
    }

    // 대기 애니메이션을 재생합니다.
    public void PlayIdle(bool force = false)
    {
        PlayLoopState(CombatUnitAnimationState.Idle, idleStateHash, force);
    }

    // 이동 애니메이션을 재생하고 이동 속도 배속을 반영합니다.
    public void PlayMove(float moveSpeed, bool force = false)
    {
        if (animator != null && hasMoveSpeedParameter)
            animator.SetFloat(MoveSpeedHash, Mathf.Max(0f, moveSpeed));

        PlayLoopState(CombatUnitAnimationState.Move, moveStateHash, force);
    }

    // 공격 애니메이션을 재생합니다.
    public void PlayAttack(float attackSpeed = 1f)
    {
        if (animator != null && hasAttackSpeedParameter)
            animator.SetFloat(AttackSpeedHash, Mathf.Max(0.01f, attackSpeed));

        PlayOneShotState(CombatUnitAnimationState.Attack, attackStateHash);
    }

    // 피격 애니메이션을 재생합니다.
    public void PlayHit()
    {
        PlayOneShotState(CombatUnitAnimationState.Hit, hitStateHash);
    }

    // 사망 애니메이션을 재생합니다.
    public void PlayDie()
    {
        PlayOneShotState(CombatUnitAnimationState.Die, dieStateHash);
    }

    // 반복 상태는 같은 상태가 계속 요청될 때 매 프레임 재시작하지 않습니다.
    private void PlayLoopState(CombatUnitAnimationState nextState, int stateHash, bool force)
    {
        if (animator == null)
            return;

        if (stateHash == 0)
            return;

        if (!force && currentState == nextState)
            return;

        currentState = nextState;
        animator.CrossFade(stateHash, Mathf.Max(0f, defaultCrossFadeSeconds), 0);
    }

    // 순간 상태는 같은 상태라도 요청이 들어오면 다시 재생합니다.
    private void PlayOneShotState(CombatUnitAnimationState nextState, int stateHash)
    {
        if (animator == null)
            return;

        if (stateHash == 0)
            return;

        currentState = nextState;
        animator.CrossFade(stateHash, Mathf.Max(0f, defaultCrossFadeSeconds), 0, 0f);
    }

    // 공통 애니메이션 요청 이름을 실제 Animator Controller에 존재하는 상태 이름으로 변환합니다.
    private int ResolveStateHash(params string[] stateNames)
    {
        if (animator == null || stateNames == null)
            return 0;

        for (int i = 0; i < stateNames.Length; i++)
        {
            string stateName = stateNames[i];

            if (string.IsNullOrWhiteSpace(stateName))
                continue;

            int shortHash = Animator.StringToHash(stateName);
            if (animator.HasState(0, shortHash))
                return shortHash;

            int fullPathHash = Animator.StringToHash($"Base Layer.{stateName}");
            if (animator.HasState(0, fullPathHash))
                return fullPathHash;
        }

        return 0;
    }

    // Animator Controller마다 파라미터가 없을 수 있으므로 지원 여부를 생성 시 한 번만 검사합니다.
    private bool HasAnimatorParameter(int parameterHash)
    {
        if (animator == null)
            return false;

        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].nameHash == parameterHash)
                return true;
        }

        return false;
    }
}
