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

    // Seviye nesneleri (ör. kırılabilirler) oyuncuya referans tutmadan yeniden doğuşu dinler
    public static event Action AnyPlayerRespawned;

    public bool IsDead { get; private set; }

    private PlayerMotor motor;
    private Renderer[] renderers;
    // Ölüm anında hangi görsellerin açık olduğu; yeniden doğuşta sadece onlar geri açılır
    private bool[] rendererStates;
    // Geçici sayaç; asıl ölüm sayacı (A.4) bölüm akışı ile gelecek
    private int deathCount;

    // Domain reload kapatılırsa statik event Play oturumları arasında kalır; bu yüzden elle temizlenir
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        AnyPlayerRespawned = null;
    }

    private void Awake()
    {
        motor = GetComponent<PlayerMotor>();
        renderers = GetComponentsInChildren<Renderer>();
        rendererStates = new bool[renderers.Length];
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

        // Önce yetenekler kendini kapatsın (ör. kılıç görseli), sonra görsel durumu kaydedilsin
        Died?.Invoke();
        motor.SetFrozen(true);
        HideRenderers();
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        motor.ResetState(SafeZone.CurrentSpawnPosition);
        motor.SetFrozen(false);
        RestoreRenderers();
        IsDead = false;

        Respawned?.Invoke();
        AnyPlayerRespawned?.Invoke();
    }

    private void HideRenderers()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            rendererStates[i] = renderers[i].enabled;
            renderers[i].enabled = false;
        }
    }

    private void RestoreRenderers()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = rendererStates[i];
        }
    }
}
