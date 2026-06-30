using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
public sealed class AssetManager : MonoBehaviour
{
    public static AssetManager Instance
    {
        get; private set;
    }

    [SerializeField]
    private Transform _poolRoot;

    private AddressableAssetLoader _assetLoader;
    private AddressableSpawner _spawner;
    private PoolManager _poolManager;

    [SerializeField]
    private AddressablePreloadKeyProvider _downloadKeyPreloadKeyProvider;
    public AddressablePreloadKeyProvider DownloadKeyPreloadKeyProvider
    {
        get { return _downloadKeyPreloadKeyProvider; }
    }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (_poolRoot == null)
        {
            GameObject poolRootObject = new GameObject("PoolRoot");
            poolRootObject.transform.SetParent(transform, false);
            _poolRoot = poolRootObject.transform;
        }

        _assetLoader = new AddressableAssetLoader();
        _spawner = new AddressableSpawner();
        _poolManager = new PoolManager(_assetLoader, _poolRoot);
    }

    public UniTask<T> LoadAssetAsync<T>(string key) where T : Object
    {
        return _assetLoader.LoadAsync<T>(key);
    }

    public void ReleaseAsset(string key)
    {
        _assetLoader.Release(key);
    }

    public UniTask<GameObject> SpawnAsync(string key, Transform parent = null)
    {
        return _spawner.SpawnAsync(key, parent);
    }

    public void ReleaseInstance(GameObject instance)
    {
        _spawner.ReleaseInstance(instance);
    }

    public UniTask<GameObject> GetFromPoolAsync(string key, Transform parent = null)
    {
        return _poolManager.GetAsync(key, parent);
    }

    public void ReturnToPool(string key, GameObject instance)
    {
        _poolManager.Return(key, instance);
    }
    private void OnDestroy()
    {
        if (Instance != this)
        {
            return;
        }

        _poolManager?.Dispose();
        _spawner?.Dispose();
        _assetLoader?.Dispose();

        Instance = null;
    }
}