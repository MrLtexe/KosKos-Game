using UnityEngine;

// Batarya (C): oyuncu içinden geçince jet-çanta deposunu tamamen doldurur ve kaybolur.
// Oyuncu yeniden doğunca geri gelir; böylece alan her denemede aynıdır.
[RequireComponent(typeof(Collider))]
public class Battery : MonoBehaviour
{
    private Collider[] colliders;
    private Renderer[] renderers;
    private bool taken;

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

    private void OnTriggerEnter(Collider other)
    {
        if (taken) return;
        Rigidbody body = other.attachedRigidbody;
        if (body == null || !body.TryGetComponent(out JetBagAbility jetBag)) return;
        // Jet-çanta kilitliyse batarya alınmaz
        if (!jetBag.Unlocked) return;

        jetBag.Refill();
        taken = true;
        SetPresent(false);
    }

    private void Restore()
    {
        if (!taken) return;
        taken = false;
        SetPresent(true);
    }

    private void SetPresent(bool present)
    {
        foreach (Collider c in colliders) c.enabled = present;
        foreach (Renderer r in renderers) r.enabled = present;
    }
}
