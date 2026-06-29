using Coffee.UIExtensions;
using System.Threading.Tasks;
using UnityEngine;

public class UISelectButton : MonoBehaviour
{
    [Header("UI Particle Effects")]
    [SerializeField] private UIParticle _selectedIdle;
    [SerializeField] private UIParticle _confirmBurst;

    private void Awake()
    {
        StopEffect(_selectedIdle);
        StopEffect(_confirmBurst);
    }

    public void SetSelected(bool selected)
    {
        if (selected)
        {
            PlayEffect(_selectedIdle, true);
        }
        else
        {
            StopEffect(_selectedIdle);
        }
    }

    private int _confirmToken;

    public void PlayConfirm()
    {
        _ = PlayConfirmAsync();
    }

    private async Awaitable PlayConfirmAsync()
    {
        _confirmBurst.gameObject.SetActive(true);

        PlayEffect(_confirmBurst, true);

        await Awaitable.WaitForSecondsAsync(1f);

        _confirmBurst.gameObject.SetActive(false);
    }

    private async Awaitable PlayConfirmAsync(int token)
    {
        _confirmBurst.gameObject.SetActive(true);

        PlayEffect(_confirmBurst, true);

        await Awaitable.WaitForSecondsAsync(1f);

        if (token != _confirmToken)
        {
            return;
        }

        _confirmBurst.gameObject.SetActive(false);
    }

    private void PlayEffect(UIParticle effect, bool restart)
    {
        if (effect == null)
        {
            return;
        }

        if (restart)
        {
            effect.Stop();
            effect.Clear();
        }

        effect.Play();
    }

    private void StopEffect(UIParticle effect)
    {
        if (effect == null)
        {
            return;
        }

        effect.Clear();
        effect.Stop();
    }
}