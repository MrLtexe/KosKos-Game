using UnityEngine;

// Zipline (D.6): başlangıçtan bitişe tek düz çizgi. Her zaman başlangıç → bitiş yönünde sürülür.
// Bu nesne sadece yeri tarif eder; sürme ve denge oyuncudaki ZiplineRider'dadır.
[RequireComponent(typeof(Collider))]
public class Zipline : MonoBehaviour
{
    [Tooltip("Hattın başlangıç noktası.")]
    [SerializeField] private Transform start;

    [Tooltip("Hattın bitiş noktası. Oyuncu burada bırakılır.")]
    [SerializeField] private Transform end;

    public Vector3 StartPoint => Flat(start.position);
    public Vector3 EndPoint => Flat(end.position);
    public float Length => Vector3.Distance(StartPoint, EndPoint);
    public Vector3 Direction => (EndPoint - StartPoint).normalized;

    public Vector3 PointAt(float distance)
    {
        return StartPoint + Direction * Mathf.Clamp(distance, 0f, Length);
    }

    public float DistanceAlong(Vector3 position)
    {
        return Mathf.Clamp(Vector3.Dot(Flat(position) - StartPoint, Direction), 0f, Length);
    }

    // Atılma bitince hâlâ tetik içindeyse tutunabilsin diye Stay de dinlenir
    private void OnTriggerEnter(Collider other) => TryGrab(other);
    private void OnTriggerStay(Collider other) => TryGrab(other);

    private void TryGrab(Collider other)
    {
        Rigidbody body = other.attachedRigidbody;
        if (body != null && body.TryGetComponent(out ZiplineRider rider))
        {
            rider.TryGrab(this);
        }
    }

    private static Vector3 Flat(Vector3 v) => new Vector3(v.x, v.y, 0f);
}
