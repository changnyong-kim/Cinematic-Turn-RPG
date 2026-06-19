using Cysharp.Threading.Tasks;
using UnityEngine;

public abstract class IntroPlatformBase
{
    public IntroPlatformBase()
    {

    }

    public virtual UniTask InitializeAsync()
    {
        return UniTask.CompletedTask;
    }

    public virtual void QuitApplication()
    {
        Application.Quit();
    }
}
