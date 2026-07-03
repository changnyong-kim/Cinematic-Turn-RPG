using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 전투 흐름을 제어하는 컨트롤러입니다.
/// 플레이어 입력, 몬스터 행동 선택, 스킬 실행 요청, 턴 전환, 전투 결과 반영을 담당합니다.
/// 실제 전투 데이터 처리는 BattleModel에 위임하고,
/// 시네마틱 연출은 BattleCinematicDirector에 위임합니다.
/// </summary>
public sealed class BattleController : MonoBehaviour, IBattleCinematicEventHandler
{
    [Header("Spawn Point")]
    [SerializeField]
    private Transform _playerSpawnPoint;

    [SerializeField]
    private Transform _monsterSpawnPoint;

    [Header("Cinematic")]
    [SerializeField]
    private BattleCinematicDirector _cinematicDirector;

    [SerializeField]
    private CinemachineBrain _cinemachineBrain;

    [Header("UI")]
    [SerializeField]
    private UIBattleView _battleView;

    /// <summary>
    /// AI가 별도로 없기 때문에, 행동순서로 몬스터 스킬을 결정한다.
    /// </summary>
    private int _monsterSkillSeqIdx;

    [Header("State Effect")]
    [SerializeField]
    private int _stunRecoveryDelayMs = 3000;

    private readonly BattleViewModel _viewModel = new BattleViewModel();
    private BattleModel _battleModel;

    #region 유니티 생명주기
    private void Awake()
    {
        if (_battleView != null)
        {
            _battleView.Bind(_viewModel);
            _battleView.OnAttackClicked += OnAttackClicked;
            _battleView.OnParryClicked += OnParryClicked;
        }
    }

    private void Start()
    {
        StartBattleAsync().Forget(Debug.LogException);
    }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        // 현재 전투 상태에 따라 공격 또는 패링으로 처리되는 원버튼 입력
        if (Keyboard.current.enterKey.wasReleasedThisFrame ||
            Keyboard.current.spaceKey.wasReleasedThisFrame)
        {
            OnAttackClicked();
            OnParryClicked();
        }
    }

    private void OnDestroy()
    {
        if (_battleView != null)
        {
            _battleView.OnAttackClicked -= OnAttackClicked;
            _battleView.OnParryClicked -= OnParryClicked;

            _battleView.Unbind();
        }
    }
    #endregion


    #region 전투 초기화
    private async UniTask StartBattleAsync()
    {
        ActorBase player = null;
        ActorBase monster = null;

        IReadOnlyList<ActorTableData> actorTableList = TableManager.Instance.ActorTableList;

        for (int i = 0; i < actorTableList.Count; i++)
        {
            ActorTableData actorData = actorTableList[i];

            switch (actorData.Type)
            {
                case ActorType.Player:
                    if (player == null)
                    {
                        player = await SpawnActorAsync(actorData, _playerSpawnPoint);
                    }
                    break;

                case ActorType.Monster:
                    if (monster == null)
                    {
                        monster = await SpawnActorAsync(actorData, _monsterSpawnPoint);
                    }
                    break;

                case ActorType.None:
                default:
                    Debug.LogError($"[BattleController] Invalid ActorType. Id: {actorData.Id}, Name: {actorData.Name}");
                    break;
            }
        }

        if (player == null || monster == null)
        {
            Debug.LogError("[BattleController] Battle start failed. Player or Monster is missing.");
            return;
        }

        _battleModel = new BattleModel(player, monster);

        if (_cinematicDirector != null)
        {
            _cinematicDirector.BindActors(_battleModel.Player, _battleModel.Monster);
            _cinematicDirector.BindEventHandler(this);
        }

        RefreshBattleView();

        await _cinematicDirector.PlayBattleStartAsync();

        _viewModel.SetTurnText(BattleTurnViewState.PlayerTurn);

        _viewModel.ShowBattleUI(useFade: true);

        _viewModel.SetAttackButtonInteractable(true);
        _viewModel.SetParryButtonInteractable(false);

        //카메라 제어권 반환
        _cinemachineBrain.enabled = false;

        Debug.Log("[BattleController] Battle Start");
    }

    private async UniTask<ActorBase> SpawnActorAsync(ActorTableData data, Transform spawnPoint)
    {
        if (data == null)
        {
            Debug.LogError("[BattleController] ActorTableData is null.");
            return null;
        }

        if (spawnPoint == null)
        {
            Debug.LogError($"[BattleController] SpawnPoint is null. Actor: {data.Name}");
            return null;
        }

        GameObject actorObject = await AssetManager.Instance.SpawnAsync(data.PrefabKey, spawnPoint);

        if (actorObject == null)
        {
            Debug.LogError($"[BattleController] Spawn failed. PrefabKey: {data.PrefabKey}");
            return null;
        }

        ActorBase actor = actorObject.GetComponent<ActorBase>();

        if (actor == null)
        {
            Debug.LogError($"[BattleController] ActorBase not found. PrefabKey: {data.PrefabKey}");
            return null;
        }

        actorObject.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
        actorObject.transform.localPosition = Vector3.zero;
        actorObject.transform.localRotation = Quaternion.identity;

        actor.Initialize(data);

        return actor;
    }
    #endregion


    #region 플레이어 입력
    private void OnAttackClicked()
    {
        if (_battleModel == null || _battleModel.CanPlayerAct == false)
        {
            return;
        }

        _viewModel.SetAttackButtonInteractable(false);

        BattleSkillTableData skillData = TableManager.Instance.GetBattleSkill(BattleSkillId.PlayerNormalAttack);
        PlaySkillSequence(BattleTeam.Ally, skillData);
    }

    public void OnParryClicked()
    {
        if (_battleModel == null || _battleModel.CanRequestParry == false)
        {
            return;
        }

        _viewModel.SetParryButtonInteractable(false);
        _battleModel.RequestParry();
    }
    #endregion


    #region 스킬 실행 흐름
    private void PlaySkillSequence(BattleTeam attackerTeam, BattleSkillTableData skillData)
    {
        if (_battleModel == null || _cinematicDirector == null || skillData == null)
        {
            return;
        }

        ActorBase attacker = GetActor(attackerTeam);
        ActorBase defender = GetOpponent(attackerTeam);

        _viewModel.SetTurnText((attackerTeam == BattleTeam.Ally) ? BattleTurnViewState.PlayerAttack : BattleTurnViewState.MonsterAttack);

        _viewModel.SetSkillNotiText(skillData.NoticeText);

        _cinematicDirector.PlayAttack(
            attackerTeam,
            attacker,
            defender,
            () => _viewModel.SetTurnUIVisible(false),
            () => ApplySkill(attackerTeam, skillData),
            OnTurnEnd);
    }

    private BattleResult ApplySkill(BattleTeam attackerTeam, BattleSkillTableData skillData)
    {
        BattleResult result = (attackerTeam == BattleTeam.Ally) ? _battleModel.UsePlayerSkill(skillData) : _battleModel.UseMonsterSkill(skillData);

        ApplyBattleResult(result);

        RefreshBattleView();

        return result;
    }
    #endregion


    #region 몬스터 행동 선택
    private void ExecuteMonsterTurn()
    {
        if (_battleModel == null || _battleModel.CanMonsterAct == false || _cinematicDirector == null)
        {
            return;
        }

        _battleModel.Player.SetBlocking(true);

        PlaySkillSequence(BattleTeam.Enemy, SelectMonsterSkill());
    }

    /// <summary>
    /// 몬스터 공격 스킬 선택, 현재는 2번쨰 공격마다 BattleSkillId : MonsterStunAttack 사용
    /// </summary>
    /// <returns></returns>
    private BattleSkillTableData SelectMonsterSkill()
    {
        _monsterSkillSeqIdx++;

        BattleSkillId skillId = (_monsterSkillSeqIdx % 3 == 0) ? BattleSkillId.MonsterStunAttack : BattleSkillId.MonsterNormalAttack;

        var skillData = TableManager.Instance.GetBattleSkill(skillId);
        //패링 불가 공격은 버튼 잠금
        _viewModel.ParryButtonInteractable.SetValue(skillData.CanParry);

        return skillData;
    }
    #endregion

    #region 턴 처리
    private BattleState OnTurnEnd()
    {
        OnTurnEndAsync().Forget(Debug.LogException);

        if (_battleModel == null)
        {
            return BattleState.None;
        }

        return _battleModel.State;
    }

    private async UniTask OnTurnEndAsync()
    {
        if (_battleModel == null)
        {
            return;
        }

        BattleResult turnStartResult = _battleModel.ResolveTurnStart();

        _viewModel.SetTurnUIVisible(true);
        _viewModel.SetCommandUIVisible(true);

        if (turnStartResult.IsStatusSkipped)
        {
            await ApplyStatusSkipResultAsync(turnStartResult);
            return;
        }

        ApplyTurnState(_battleModel.State);
    }

    private void ApplyTurnState(BattleState battleState)
    {
        switch (battleState)
        {
            case BattleState.PlayerTurn:
            {
                _viewModel.SetTurnText(BattleTurnViewState.PlayerTurn);

                _battleModel.Player.SetBlocking(false);
                _battleModel.Monster.ActiveAuraParticle(true);

                _viewModel.SetAttackButtonInteractable(true);
                _viewModel.SetParryButtonInteractable(false);

                break;
            }
            case BattleState.MonsterTurn:
            {
                _battleModel.Monster.ActiveAuraParticle(true);

                _viewModel.SetAttackButtonInteractable(false);
                _viewModel.SetParryButtonInteractable(true);

                ExecuteMonsterTurn();
                break;
            }
            default:
            {
                break;
            }
        }
    }
    #endregion


    #region 상태이상 처리
    private async UniTask ApplyStatusSkipResultAsync(BattleResult result)
    {
        switch (result.SkippedStatusType)
        {
            case ActorStatusType.Stun:
            {
                await ApplyStunSkipResultAsync(result);
                break;
            }

            default:
            {
                ApplyTurnState(result.State);
                break;
            }
        }
    }

    private async UniTask ApplyStunSkipResultAsync(BattleResult result)
    {
        _viewModel.SetAttackButtonInteractable(false);
        _viewModel.SetParryButtonInteractable(false);

        if (result.State == BattleState.PlayerTurn)
        {
            _viewModel.SetTurnText(BattleTurnViewState.MonsterStunned);

            _battleModel.Player.SetBlocking(false);
            _battleModel.Monster.Stunned();
            _battleModel.Monster.ActiveAuraParticle(false);

            await UniTask.Delay(_stunRecoveryDelayMs);

            ApplyTurnState(result.State);
            return;
        }

        if (result.State == BattleState.MonsterTurn)
        {
            _viewModel.SetTurnText(BattleTurnViewState.PlayerStunned);

            _battleModel.Player.SetBlocking(false);
            _battleModel.Player.Stunned();
            _battleModel.Monster.ActiveAuraParticle(true);

            await UniTask.Delay(_stunRecoveryDelayMs);

            _battleModel.Player.ForceIdle();

            ApplyTurnState(result.State);
            return;
        }

        ApplyTurnState(result.State);
    }
    #endregion


    #region 액터 조회
    private ActorBase GetActor(BattleTeam team)
    {
        return team == BattleTeam.Ally
            ? _battleModel.Player
            : _battleModel.Monster;
    }

    private ActorBase GetOpponent(BattleTeam team)
    {
        return team == BattleTeam.Ally
            ? _battleModel.Monster
            : _battleModel.Player;
    }
    #endregion


    #region 타임라인 이벤트 핸들러 콜백
    public void OnParryWindowOpened()
    {
        if (_battleModel == null)
        {
            return;
        }

        _battleModel.OpenParryWindow();
    }

    public void OnParryWindowClosed()
    {
        if (_battleModel == null)
        {
            return;
        }

        _battleModel.CloseParryWindow();
    }

    public void OnParrySucceeded()
    {
        _viewModel.SetCommandUIVisible(false);
        _viewModel.SetTurnUIVisible(false);
    }

    public void OnParryEnd()
    {
        BattleSkillTableData skillData = TableManager.Instance.GetBattleSkill(BattleSkillId.PlayerNormalAttack);
        
        //패리 데미지 적용
        ApplySkill(BattleTeam.Ally, skillData);

        if (_battleModel.State != BattleState.Win)
        {
            //적 전장 복귀
           _cinematicDirector.OnParryEndAsync().Forget();
        }
    }
    #endregion


    #region 전투 결과 및 뷰 갱신
    private void ApplyBattleResult(BattleResult result)
    {
        switch (result.State)
        {
            case BattleState.Win:
            {
                _viewModel.SetTurnText(BattleTurnViewState.Win);
                _viewModel.SetTurnUIVisible(true);
                _viewModel.SetAttackButtonInteractable(false);
                break;
            }
            case BattleState.Lose:
            {
                _viewModel.SetTurnText(BattleTurnViewState.Lose);
                _viewModel.SetTurnUIVisible(true);
                _viewModel.SetAttackButtonInteractable(false);
                break;
            }
            default:
            {
                break;
            }
        }
    }

    private void RefreshBattleView()
    {
        if (_battleModel == null)
        {
            return;
        }

        _viewModel.SetPlayerHp(_battleModel.Player.CurrentHp, _battleModel.Player.MaxHp);
        _viewModel.SetMonsterHp(_battleModel.Monster.CurrentHp, _battleModel.Monster.MaxHp);
    }
    #endregion
}
