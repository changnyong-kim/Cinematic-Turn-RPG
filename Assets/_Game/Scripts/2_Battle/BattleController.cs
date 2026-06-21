using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using static UnityEditor.Experimental.GraphView.GraphView;

public class BattleController : MonoBehaviour
{
    private readonly BattleActorMover _actorMover = new();

    [Header("Spawn Point")]
    [SerializeField]
    private Transform _playerSpawnPoint;

    [SerializeField]
    private Transform _monsterSpawnPoint;

    [SerializeField]
    private PlayableDirector _playerAttackDirector;

    [SerializeField]
    private PlayableDirector _monsterAttackDirector;

    [SerializeField]
    private PlayableDirector _playerHitDirector, _monsterHitDirector;

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
        await SceneManager.LoadSceneAsync("MedievalCastle", LoadSceneMode.Additive).ToUniTask();

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

        BindTimelineActors();

        RefreshBattleView();
        _viewModel.SetTurnText("Player Turn");
        _viewModel.SetAttackButtonInteractable(true);

        Debug.Log("[BattleController] Battle Start");
    }

    private void BindTimelineActors()
    {
        BindAnimator(_playerAttackDirector, "PlayerAnimationTrack", _battleModel.Player);
        BindAnimator(_monsterAttackDirector, "MonsterAnimationTrack", _battleModel.Monster);
        BindAnimator(_playerHitDirector, "AnimationTrack", _battleModel.Player);
        BindAnimator(_monsterHitDirector, "AnimationTrack", _battleModel.Monster);
    }

    private void BindAnimator(PlayableDirector director, string trackName, ActorBase actor)
    {
        if (director == null || actor == null)
        {
            return;
        }

        Animator animator = actor.GetAnimator;

        if (animator == null)
        {
            Debug.LogError($"[BattleController] Animator not found. Actor: {actor.name}");
            return;
        }

        foreach (PlayableBinding binding in director.playableAsset.outputs)
        {
            if (binding.streamName == trackName)
            {
                director.SetGenericBinding(binding.sourceObject, animator);
                return;
            }
        }

        Debug.LogError($"[BattleController] Timeline track not found. TrackName: {trackName}");
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


    #region 플레이어 전투 타임라인 시퀀스

    private Vector3 _playerOriginPosition;

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

        _playerOriginPosition = _battleModel.Player.transform.position;

        await _actorMover.MoveToTargetAsync(
            _battleModel.Player.GetAnimator,
            _battleModel.Player.transform,
            _battleModel.Monster.transform,
             2.5f, 1.5f);

        _playerAttackDirector.Play();
    }

    private async UniTask ReturnPlayerAsync()
    {
        await _actorMover.ReturnAsync(
            _battleModel.Player.GetAnimator,
            _battleModel.Player.transform,
            _playerOriginPosition,
            1.5f);

        await ExecuteMonsterTurn();
    }
    #endregion


    #region 몬스터 타임라인 시퀀스
    private async UniTask ExecuteMonsterTurn()
    {
        if (_battleModel == null || _battleModel.CanMonsterAct == false)
        {
            return;
        }

        _viewModel.SetTurnText("Monster Attack");

        _monsterOriginPosition = _battleModel.Monster.transform.position;

        await _actorMover.MoveToTargetAsync(
            _battleModel.Monster.GetAnimator,
            _battleModel.Monster.transform,
            _battleModel.Player.transform,
            1.5f, 1f);

        _monsterAttackDirector.Play();
    }

    private BattleResult _lastMonsterAttackResult;
    private Vector3 _monsterOriginPosition;

    private async UniTask ReturnMonsterAsync()
    {
        await _actorMover.ReturnAsync(
            _battleModel.Monster.GetAnimator,
            _battleModel.Monster.transform,
            _monsterOriginPosition,
            .8f);

        _viewModel.SetTurnText("Player Turn");
        _viewModel.SetAttackButtonInteractable(true);
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

    #region 플레이어 타임라인 시그널 리시버
    private BattleResult _lastAttackResult;

    public void OnPlayerAttackApplyDamage()
    {
        _lastAttackResult = _battleModel.PlayerAttack();

        RefreshBattleView();

        ApplyBattleResult(_lastAttackResult);

        //몬스터 히트리액션
        _monsterHitDirector.Play();
    }

    public void OnPlayerAttackEnd()
    {
        if (_lastAttackResult.IsFinished)
        {
            return;
        }

        ReturnPlayerAsync().Forget();
    }
    #endregion

    #region 몬스터 타임라인 시그널 리시버
    public void OnMonsterAttackApplyDamage()
    {
        _lastMonsterAttackResult = _battleModel.MonsterAttack();

        RefreshBattleView();

        ApplyBattleResult(_lastMonsterAttackResult);

        //플레이어 히트리액션
        _playerHitDirector.Play();
    }

    public void OnMonsterAttackEnd()
    {
        if (_lastMonsterAttackResult.IsFinished)
        {
            return;
        }

        ReturnMonsterAsync().Forget();
    }
    #endregion
}
