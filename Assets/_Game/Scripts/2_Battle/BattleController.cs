using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.LowLevelPhysics2D.PhysicsQuery;

public sealed class BattleController : MonoBehaviour
{
    [Header("Spawn Point")]
    [SerializeField]
    private Transform _playerSpawnPoint;

    [SerializeField]
    private Transform _monsterSpawnPoint;

    [Header("Cinematic")]
    [SerializeField]
    private BattleCinematicDirector _cinematicDirector;

    [Header("UI")]
    [SerializeField]
    private UIBattleView _battleView;

    private readonly BattleViewModel _viewModel = new BattleViewModel();
    private BattleModel _battleModel;

    private void Awake()
    {
        if (_battleView != null)
        {
            _battleView.Bind(_viewModel);
            _battleView.OnAttackClicked += OnAttackClicked;
        }
    }

    private void Start()
    {
        StartBattleAsync().Forget(Debug.LogException);
    }

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
            _viewModel.SetTurnText("Battle Start Failed");
            _viewModel.SetAttackButtonInteractable(false);
            return;
        }

        _battleModel = new BattleModel(player, monster);

        if (_cinematicDirector != null)
        {
            _cinematicDirector.BindActors(_battleModel.Player, _battleModel.Monster);
        }

        RefreshBattleView();
        _viewModel.SetTurnText("Player Turn");
        _viewModel.SetAttackButtonInteractable(true);

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

    #region 플레이어 입력 액션
    private void OnAttackClicked()
    {
        if (_battleModel == null || _battleModel.CanPlayerAct == false)
        {
            return;
        }

        _viewModel.SetAttackButtonInteractable(false);
        PlayAttackSequenceAsync(BattleTeam.Ally);
    }

    public void OnParryInput()
    {
        if (_battleModel.CanRequestParry == false)
        {
            return;
        }

        _battleModel.RequestParry();

        _battleModel.Player.SetBlocking(true);
        // 또는 ParryReadyDirector 재생
    }
    #endregion


    private void PlayAttackSequenceAsync(BattleTeam attackerTeam)
    {
        if (_battleModel == null || _cinematicDirector == null)
        {
            return;
        }

        ActorBase attacker = GetActor(attackerTeam);
        ActorBase defender = GetOpponent(attackerTeam);

        _viewModel.SetTurnText(attackerTeam == BattleTeam.Ally
            ? "Player Attack"
            : "Monster Attack");

        _cinematicDirector.PlayAttack(
            attackerTeam,
            attacker,
            defender,
            () => ApplyAttackDamage(attackerTeam),
            OnTurnEnd);
    }

    private void ExecuteMonsterTurnAsync()
    {
        if (_battleModel == null || _battleModel.CanMonsterAct == false || _cinematicDirector == null)
        {
            return;
        }

        _battleModel.OpenParryWindow();

        //_battleModel.Player.SetBlocking(true);
        PlayAttackSequenceAsync(BattleTeam.Enemy);
    }

    private BattleState OnTurnEnd()
    {
        BattleState battleState = _battleModel.State;

        switch (battleState)
        {
            case BattleState.PlayerTurn:
                {
                    _viewModel.SetTurnText("Player Turn");
                    _battleModel.Player.SetBlocking(false);
                    _viewModel.SetAttackButtonInteractable(true);

                    //PlayAttackSequenceAsync();
                    break;
                }
            case BattleState.MonsterTurn:
                {
                    //_battleModel.Player.SetBlocking(true);
                    ExecuteMonsterTurnAsync();
                    break;
                }
            default:
                {
                    break;
                }
        }

        return battleState;
    }

    private BattleResult ApplyAttackDamage(BattleTeam attackerTeam)
    {
        BattleResult result = attackerTeam == BattleTeam.Ally
            ? _battleModel.PlayerAttack()
            : _battleModel.MonsterAttack();

        RefreshBattleView();
        ApplyBattleResult(result);

        return result;
    }

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

    private void ApplyBattleResult(BattleResult result)
    {
        switch (result.State)
        {
            case BattleState.Win:
                _viewModel.SetTurnText("Win");
                _viewModel.SetAttackButtonInteractable(false);
                break;

            case BattleState.Lose:
                _viewModel.SetTurnText("Lose");
                _viewModel.SetAttackButtonInteractable(false);
                break;
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

    private void OnDestroy()
    {
        if (_battleView != null)
        {
            _battleView.OnAttackClicked -= OnAttackClicked;
            _battleView.Unbind();
        }
    }
}