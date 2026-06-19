using System;
using Cysharp.Threading.Tasks;

public class AddressableAssetLoader : IDisposable
{
    public void Dispose()
    {
        //throw new NotImplementedException();
    }

    internal UniTask<T> LoadAsync<T>(string key) where T : UnityEngine.Object
    {
        throw new NotImplementedException();
    }

    internal void Release(string key)
    {
        throw new NotImplementedException();
    }
}