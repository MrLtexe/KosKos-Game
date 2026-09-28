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

    public static Vector3 CurrentSpawnPosition => current != null ? current.spawnPoint.position : Vector3.zero;

    // Domain reload kapalı olduğu için statik alan Play oturumları arasında elle sıfırlanmalı
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        current = null;
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
        }
    }
}
