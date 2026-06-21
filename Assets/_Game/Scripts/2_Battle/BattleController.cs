using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

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

    private void OnAttackClicked()
    {
        if (_battleModel == null || _battleModel.CanPlayerAct == false)
        {
            return;
        }

        _viewModel.SetAttackButtonInteractable(false);
        PlayAttackSequenceAsync();
    }

    private void PlayAttackSequenceAsync()
    {
        if (_battleModel == null || _cinematicDirector == null)
        {
            return;
        }

        _viewModel.SetTurnText("Player Attack");

        _cinematicDirector.PlayPlayerAttackAsync(
            _battleModel.Player,
            _battleModel.Monster,
            ApplyPlayerAttackDamage,
            OnTurnEnd);
    }

    private BattleResult ApplyPlayerAttackDamage()
    {
        BattleResult result = _battleModel.PlayerAttack();

        RefreshBattleView();
        ApplyBattleResult(result);

        return result;
    }

    private BattleState OnTurnEnd()
    {
        BattleState battleState = _battleModel.State;

        switch (battleState)
        {
            case BattleState.PlayerTurn:
                {
                    _viewModel.SetTurnText("Player Turn");
                    _viewModel.SetAttackButtonInteractable(true);

                    //PlayAttackSequenceAsync();
                    break;
                }
            case BattleState.MonsterTurn:
                {
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

    private void ExecuteMonsterTurnAsync()
    {
        if (_battleModel == null || _battleModel.CanMonsterAct == false || _cinematicDirector == null)
        {
            return;
        }

        _viewModel.SetTurnText("Monster Attack");

        _cinematicDirector.PlayMonsterAttackAsync(
            _battleModel.Monster,
            _battleModel.Player,
            ApplyMonsterAttackDamage,
            OnTurnEnd);
    }

    private BattleResult ApplyMonsterAttackDamage()
    {
        BattleResult result = _battleModel.MonsterAttack();

        RefreshBattleView();
        ApplyBattleResult(result);

        return result;
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
