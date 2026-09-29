using System;
using UnityEngine;

// Düşman gövdesi: ölüm, oyuncu yeniden doğunca geri gelme ve temas ölümü.
// Dron: kılıçla öldürülebilir. Tavan silahı: sadece savuşturulan mermiyle.
[RequireComponent(typeof(Collider))]
public class EnemyBody : MonoBehaviour, ISwordHittable
{
    [Tooltip("Kılıç doğrudan öldürebilir mi? Dron: evet. Tavan silahı: hayır (sadece geri dönen mermi öldürür).")]
    [SerializeField] private bool swordCanKill;

    // Önce bu event tetiklenir (ör. nişan çizgisi gizlenir), sonra görseller kapatılır
    public event Action Killed;

    public bool IsDead { get; private set; }

    private Collider[] colliders;
    private Renderer[] renderers;
    // Ölüm anında açık olan görseller; geri gelince sadece onlar açılır
    private bool[] rendererStates;

    private void Awake()
    {
        colliders = GetComponentsInChildren<Collider>();
        renderers = GetComponentsInChildren<Renderer>();
        rendererStates = new bool[renderers.Length];
    }

    // Bileşen kapatılmaz; kapatılırsa yeniden doğuş event'ini dinleyemez
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
        if (!swordCanKill || IsDead) return false;
        Kill();
        return true;
    }

    public void Kill()
    {
        if (IsDead) return;
        IsDead = true;
        Killed?.Invoke();

        foreach (Collider c in colliders) c.enabled = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            rendererStates[i] = renderers[i].enabled;
            renderers[i].enabled = false;
        }
    }

    private void Restore()
    {
        if (!IsDead) return;
        IsDead = false;

        foreach (Collider c in colliders) c.enabled = true;
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = rendererStates[i];
        }
    }

    // Gövdeye her yönden temas oyuncuyu öldürür (drona basılamaz)
    private void OnTriggerEnter(Collider other)
    {
        if (IsDead) return;
        Rigidbody body = other.attachedRigidbody;
        if (body != null && body.TryGetComponent(out PlayerDeath death))
        {
            death.Die();
        }
    }
}
