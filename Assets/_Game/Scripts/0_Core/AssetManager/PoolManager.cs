using System;
using UnityEngine;

using Cysharp.Threading.Tasks;
public class PoolManager : IDisposable
{
    public PoolManager(AddressableAssetLoader addressableAssetLoader, Transform root)
    {

    }

    public void Dispose()
    {
        //throw new NotImplementedException();
    }

    public UniTask<GameObject> GetAsync(string key, Transform parent)
    {
        return UniTask.FromResult<GameObject>(null);
    }

    public void Return(string key, GameObject instance)
    {
        throw new NotImplementedException();
    }
}