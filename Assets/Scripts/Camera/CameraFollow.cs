using UnityEngine;

// Basit takip kamerası: X'te oyuncuya kilitli (oyuncu solda kalır, ileri görülür), Y'de yumuşak takip.
public class CameraFollow : MonoBehaviour
{
    [Tooltip("Takip edilecek oyuncu.")]
    [SerializeField] private Transform target;

    [Tooltip("Oyuncuya göre kamera konumu. X pozitif = oyuncu ekranın solunda kalır, Z negatif = kamera geride.")]
    [SerializeField] private Vector3 offset = new Vector3(4f, 1.5f, -14f);

    [Tooltip("Dikey takibin yumuşaklığı (sn). Büyük değer = daha yavaş, daha yumuşak.")]
    [SerializeField] private float verticalSmoothTime = 0.25f;

    private float verticalVelocity;
    private PlayerDeath targetDeath;

    private void Awake()
    {
        targetDeath = target.GetComponent<PlayerDeath>();
    }

    private void OnEnable()
    {
        if (targetDeath != null) targetDeath.Respawned += SnapToTarget;
    }

    private void OnDisable()
    {
        if (targetDeath != null) targetDeath.Respawned -= SnapToTarget;
    }

    private void Start()
    {
        SnapToTarget();
    }

    private void LateUpdate()
    {
        Vector3 goal = target.position + offset;
        float y = Mathf.SmoothDamp(transform.position.y, goal.y, ref verticalVelocity, verticalSmoothTime);
        transform.position = new Vector3(goal.x, y, goal.z);
    }

    // Yeniden doğuşta kamera kaymadan direkt oyuncuya atlar
    private void SnapToTarget()
    {
        transform.position = target.position + offset;
        verticalVelocity = 0f;
    }
}
