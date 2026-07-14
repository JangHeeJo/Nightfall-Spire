using System;
using UnityEngine;

// 히어로와 몬스터가 공유하는 전투 애니메이션 재생기입니다.
// 프리팹에는 Animator만 있으면 되고, 전투 View 계층은 이 클래스를 통해 공통 상태 이름만 요청합니다.
public sealed class CombatUnitAnimationPlayer
{
    private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed"); // 이동 애니메이션 배속 파라미터입니다.
    private static readonly int AttackSpeedHash = Animator.StringToHash("AttackSpeed"); // 공격 애니메이션 배속 파라미터입니다.
    private const string IdleStateName = "Idle"; // 공통 대기 상태 이름입니다.
    private const string WalkStateName = "Walk"; // 공통 이동 상태 이름입니다.
    private const string AttackStateName = "Attack"; // 공통 공격 상태 이름입니다.
    private const string Dead1StateName = "Dead1"; // 공통 사망 상태 이름입니다.
    private const string SkillStateName = "Skill"; // 영웅과 보스 전용 스킬 상태 이름입니다.

    private readonly Animator[] animators; // 프리팹 안의 모든 Unity Animator입니다.
    private readonly float defaultCrossFadeSeconds; // 반복 상태 전환에 사용할 기본 블렌딩 시간입니다.

    private CombatUnitAnimationState currentState = CombatUnitAnimationState.Idle; // 마지막으로 요청된 애니메이션 상태입니다.

    public CombatUnitAnimationState CurrentState => currentState; // 외부 확인용 현재 애니메이션 상태입니다.

    // Animator와 공통 전환 시간을 받아 재생기를 구성합니다.
    public CombatUnitAnimationPlayer(Animator animator, float defaultCrossFadeSeconds = 0.06f)
        : this(animator == null ? Array.Empty<Animator>() : new[] { animator }, defaultCrossFadeSeconds)
    {
    }

    // 프리팹에 Animator가 여러 개 있는 경우 모든 Animator에 같은 상태 전환을 요청합니다.
    public CombatUnitAnimationPlayer(Animator[] animators, float defaultCrossFadeSeconds = 0.06f)
    {
        this.animators = animators ?? Array.Empty<Animator>();
        this.defaultCrossFadeSeconds = defaultCrossFadeSeconds;
    }

    // 풀에서 다시 꺼낼 때 이전 트리거와 재생 시간을 초기화합니다.
    public void ResetPlayback()
    {
        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];

            if (animator == null)
                continue;

            animator.Rebind();
            animator.Update(0f);
            animator.speed = 1f;
        }

        currentState = CombatUnitAnimationState.Idle;
        PlayIdle(true);
    }

    // 드래프트 중 전투 표시 애니메이션을 멈추거나 다시 재생합니다.
    public void SetPlaybackSpeed(float speed)
    {
        float normalizedSpeed = Mathf.Max(0f, speed);

        for (int i = 0; i < animators.Length; i++)
        {
            if (animators[i] != null)
                animators[i].speed = normalizedSpeed;
        }
    }

    // 대기 애니메이션을 재생합니다.
    public void PlayIdle(bool force = false)
    {
        PlayLoopState(CombatUnitAnimationState.Idle, IdleStateName, force);
    }

    // 이동 애니메이션을 재생하고 이동 속도 배속을 반영합니다.
    public void PlayWalk(float moveSpeed, bool force = false)
    {
        SetFloatParameter(MoveSpeedHash, Mathf.Max(0f, moveSpeed));

        PlayLoopState(CombatUnitAnimationState.Walk, WalkStateName, force);
    }

    // 공격 애니메이션을 재생합니다.
    public void PlayAttack(float attackSpeed = 1f)
    {
        SetFloatParameter(AttackSpeedHash, Mathf.Max(0.01f, attackSpeed));

        PlayOneShotState(CombatUnitAnimationState.Attack, AttackStateName);
    }

    // 사망 애니메이션을 재생합니다.
    public void PlayDead1()
    {
        PlayOneShotState(CombatUnitAnimationState.Dead1, Dead1StateName);
    }

    // 영웅과 보스 스킬 구현 시 사용할 Skill 애니메이션을 재생합니다.
    public void PlaySkill(float skillSpeed = 1f)
    {
        SetFloatParameter(AttackSpeedHash, Mathf.Max(0.01f, skillSpeed));

        PlayOneShotState(CombatUnitAnimationState.Skill, SkillStateName);
    }

    // 반복 상태는 같은 상태가 계속 요청될 때 매 프레임 재시작하지 않습니다.
    private void PlayLoopState(CombatUnitAnimationState nextState, string stateName, bool force)
    {
        if (!force && currentState == nextState)
            return;

        if (CrossFadeAll(stateName, 0f))
            currentState = nextState;
    }

    // 순간 상태는 같은 상태라도 요청이 들어오면 다시 재생합니다.
    private void PlayOneShotState(CombatUnitAnimationState nextState, string stateName)
    {
        if (CrossFadeAll(stateName, 0f))
            currentState = nextState;
    }

    // 모든 Animator에 같은 상태 이름을 직접 재생 요청합니다.
    private bool CrossFadeAll(string stateName, float normalizedTime)
    {
        if (string.IsNullOrWhiteSpace(stateName))
            return false;

        bool playedAny = false;
        float crossFadeSeconds = Mathf.Max(0f, defaultCrossFadeSeconds);
        string fullPathStateName = $"Base Layer.{stateName}";

        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];

            if (animator == null || !HasState(animator, stateName))
                continue;

            animator.CrossFade(fullPathStateName, crossFadeSeconds, 0, normalizedTime);
            playedAny = true;
        }

        return playedAny;
    }

    // Animator Controller에 상태가 있는지 확인합니다.
    private static bool HasState(Animator animator, string stateName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(stateName))
            return false;

        int fullPathHash = Animator.StringToHash($"Base Layer.{stateName}");

        if (animator.HasState(0, fullPathHash))
            return true;

        return animator.HasState(0, Animator.StringToHash(stateName));
    }

    // 지원하는 Animator에만 Float 파라미터를 전달합니다.
    private void SetFloatParameter(int parameterHash, float value)
    {
        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];

            if (animator != null && HasAnimatorParameter(animator, parameterHash))
                animator.SetFloat(parameterHash, value);
        }
    }

    // Animator Controller마다 파라미터가 없을 수 있으므로 지원 여부를 검사합니다.
    private static bool HasAnimatorParameter(Animator animator, int parameterHash)
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
