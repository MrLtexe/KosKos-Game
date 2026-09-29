using System;
using UnityEngine;

// Güvenli alan: oyuncu girince yeni doğma noktası olur.
// Sadece ileri doğru ilerlenebilir; daha geride kalan bir alan mevcut alanı geçersiz kılamaz.
[RequireComponent(typeof(Collider))]
public class SafeZone : MonoBehaviour
{
    [Tooltip("Oyuncunun öldükten sonra doğacağı nokta.")]
    [SerializeField] private Transform spawnPoint;

    [Tooltip("İşaretliyse bölüm başında aktif güvenli alan budur. Sahnede sadece bir tane olmalı.")]
    [SerializeField] private bool isStartZone;

    private static SafeZone current;

    // Yeni bir güvenli alana ulaşıldı (başlangıç alanı hariç). Bölüm akışı toplanabilirleri güvenceye almak için dinler.
    public static event Action<SafeZone> Activated;

    public static Vector3 CurrentSpawnPosition
    {
        get
        {
            if (current == null)
            {
                // Sessizce (0,0,0)'da doğmak yerine sorunu görünür yap
                Debug.LogError("[KosKos] Aktif güvenli alan yok! Sahnede 'isStartZone' işaretli bir SafeZone olmalı.");
                return Vector3.zero;
            }
            return current.spawnPoint.position;
        }
    }

    // Domain reload kapatılırsa statik alan Play oturumları arasında kalır; bu yüzden elle sıfırlanır
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        current = null;
        Activated = null;
    }

    private void Awake()
    {
        if (isStartZone) current = this;
    }

    private void OnDestroy()
    {
        if (current == this) current = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        Rigidbody body = other.attachedRigidbody;
        if (body == null || !body.TryGetComponent(out PlayerDeath _)) return;

        // Geri dönülemez: sadece daha ileride olan alan aktif olabilir
        if (current == null || transform.position.x > current.transform.position.x)
        {
            current = this;
            Debug.Log($"[KosKos] Güvenli alan: {name}");
            Activated?.Invoke(this);
        }
    }
}
