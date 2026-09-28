using System;
using System.Collections;
using UnityEngine;

// Tüm ölüm sebepleri buradan geçer (önden çarpma, boşluk, boost patlaması).
// Karakteri dondurur, gizler, bekler ve son güvenli alanda yeniden doğurur.
[RequireComponent(typeof(PlayerMotor))]
public class PlayerDeath : MonoBehaviour
{
    [Tooltip("Ölümden sonra yeniden doğmadan önce beklenen süre (sn).")]
    [SerializeField] private float respawnDelay = 0.5f;

    public event Action Died;
    public event Action Respawned;

    public bool IsDead { get; private set; }

    private PlayerMotor motor;
    private Renderer[] renderers;
    // Geçici sayaç; asıl ölüm sayacı (A.4) bölüm akışı ile gelecek
    private int deathCount;

    private void Awake()
    {
        motor = GetComponent<PlayerMotor>();
        renderers = GetComponentsInChildren<Renderer>();
    }

    private void OnEnable()
    {
        motor.HitObstacle += Die;
    }

    private void OnDisable()
    {
        motor.HitObstacle -= Die;
    }

    public void Die()
    {
        if (IsDead) return;

        IsDead = true;
        deathCount++;
        Debug.Log($"[KosKos] Ölüm #{deathCount}");

        Died?.Invoke();
        motor.SetFrozen(true);
        SetVisible(false);
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        motor.ResetState(SafeZone.CurrentSpawnPosition);
        motor.SetFrozen(false);
        SetVisible(true);
        IsDead = false;

        Respawned?.Invoke();
    }

    private void SetVisible(bool visible)
    {
        foreach (Renderer r in renderers)
        {
            r.enabled = visible;
        }
    }
}
