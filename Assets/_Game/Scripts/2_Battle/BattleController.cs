using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class BattleController : MonoBehaviour
{
    [Header("Spawn Point")]
    [SerializeField]
    private Transform _playerSpawnPoint;

    [SerializeField]
    private Transform _monsterSpawnPoint;

    [SerializeField]
    private PlayableDirector _playerAttackDirector;

    [SerializeField]
    private PlayableDirector _monsterAttackDirector;

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

    /*
    private void Update()
    {

        if (Input.GetKeyDown(KeyCode.Space))
        {
            OnAttackClicked();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            ExecuteMonsterTurn();
        }
    }
    */

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

        actorObject.transform.SetPositionAndRotation( spawnPoint.position, spawnPoint.rotation);

        actor.Initialize(data);

        actorObject.transform.localPosition = Vector3.zero;
        actorObject.transform.localRotation = Quaternion.identity;

        return actor;
    }

    private async void OnAttackClicked()
    { 
        if (_battleModel == null || _battleModel.CanPlayerAct == false)
        {
            return;
        }

        _viewModel.SetAttackButtonInteractable(false);

        await PlayAttackSequenceAsync();
    }

    private void ExecuteMonsterTurn()
    {
        if (_battleModel == null || _battleModel.CanMonsterAct == false)
        {
            return;
        }

        _viewModel.SetTurnText("Monster Attack");

        BattleResult result = _battleModel.MonsterAttack();
        RefreshBattleView();
        ApplyBattleResult(result);

        if (result.IsFinished)
        {
            return;
        }

        _viewModel.SetTurnText("Player Turn");
        _viewModel.SetAttackButtonInteractable(true);
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

    #region 전투 타임라인 시퀀스
    private async UniTask PlayAttackSequenceAsync()
    {
        /*
     * TODO: Replace delay-based attack sequence with Timeline. 타임라인 교체시 작업설계도
     *
     * Current Flow:
     * Player Attack Animation
     *      ↓
     * Wait Hit Timing (Delay)
     *      ↓
     * Apply Damage
     *      ↓
     * Enemy Hit Animation
     *      ↓
     * Execute Monster Turn
     *
     * Timeline Flow:
     * PlayableDirector.Play()
     *      ↓
     * Timeline Animation Track
     *      ↓
     * Signal: PlayerAttackHit
     *      ↓
     * BattleModel.PlayerAttack()
     * Enemy.PlayHit()
     * Refresh Battle View
     *      ↓
     * Signal: PlayerAttackEnd
     *      ↓
     * Execute Monster Turn
     */


        _viewModel.SetTurnText("Player Attack");

        // TODO: Timeline Signal로 교체할 임시 구간
        await WaitHitTimingAsync();

        BattleResult result = _battleModel.PlayerAttack();

        RefreshBattleView();

        ApplyBattleResult(result);

        if (result.IsFinished)
        {
            return;
        }
    }

    [SerializeField]
    int _hitTimeDelay = 2000;
    private async UniTask WaitHitTimingAsync()
    {
        await UniTask.Delay(_hitTimeDelay);

        ExecuteMonsterTurn();
    }

    public void OnPlayerAttackHit()
    {
        BattleResult result = _battleModel.PlayerAttack();

        _battleModel.Monster.PlayHit();

        RefreshBattleView();
        ApplyBattleResult(result);
    }

    public void OnPlayerAttackEnd()
    {
        ExecuteMonsterTurn();
    }
    #endregion

    private void OnDestroy()
    {
        if (_battleView != null)
        {
            _battleView.OnAttackClicked -= OnAttackClicked;
            _battleView.Unbind();
        }
    }
}
