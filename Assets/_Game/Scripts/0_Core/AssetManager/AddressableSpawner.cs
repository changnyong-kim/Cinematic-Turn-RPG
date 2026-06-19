using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;

public class AddressableSpawner : IDisposable
{
    private readonly Dictionary<GameObject, AsyncOperationHandle<GameObject>> _instanceHandles = new();

    public async UniTask<GameObject> SpawnAsync(string key, Transform parent)
    {
        AsyncOperationHandle<GameObject> handle =
            Addressables.InstantiateAsync(key, parent, false);

        await handle.ToUniTask();

        GameObject instance = handle.Result;

        if (instance != null)
        {
            _instanceHandles[instance] = handle;
        }

        return instance;
    }

    public void ReleaseInstance(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        if (_instanceHandles.TryGetValue(instance, out AsyncOperationHandle<GameObject> handle))
        {
            Addressables.ReleaseInstance(handle);
            _instanceHandles.Remove(instance);
            return;
        }

        Debug.LogWarning($"[AddressableSpawner] Release failed. Instance was not created by this spawner: {instance.name}");
    }

    public void Dispose()
    {
        foreach (var pair in _instanceHandles)
        {
            Addressables.ReleaseInstance(pair.Value);
        }

        _instanceHandles.Clear();
    }
}