using UnityEngine;

// Kırılabilir nesne (D.3): çarpınca öldürür, ama kılıçla veya boost'luyken çarpılınca kırılır.
// Oyuncu yeniden doğunca geri gelir; böylece alan her denemede aynıdır.
[RequireComponent(typeof(Collider))]
public class Breakable : MonoBehaviour, ISwordHittable
{
    public bool IsBroken { get; private set; }

    private Collider[] colliders;
    private Renderer[] renderers;

    private void Awake()
    {
        colliders = GetComponentsInChildren<Collider>();
        renderers = GetComponentsInChildren<Renderer>();
    }

    // GameObject kapatılmaz; kapatılırsa yeniden doğuş event'ini dinleyemez
    private void OnEnable()
    {
        PlayerDeath.AnyPlayerRespawned += Restore;
    }

    private void OnDisable()
    {
        PlayerDeath.AnyPlayerRespawned -= Restore;
    }

    public bool OnSwordHit(SwordAttack source)
    {
        Break();
        return true;
    }

    public void Break()
    {
        if (IsBroken) return;
        IsBroken = true;
        SetPresent(false);
    }

    private void Restore()
    {
        if (!IsBroken) return;
        IsBroken = false;
        SetPresent(true);
    }

    private void SetPresent(bool present)
    {
        foreach (Collider c in colliders) c.enabled = present;
        foreach (Renderer r in renderers) r.enabled = present;
    }
}
