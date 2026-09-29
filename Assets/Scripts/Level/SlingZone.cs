using UnityEngine;

// Sapan alanı (D.7): oyuncu havadayken fiziksel olarak girince SlingAbility devreye girer.
[RequireComponent(typeof(Collider))]
public class SlingZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other) => TryEnter(other);
    private void OnTriggerStay(Collider other) => TryEnter(other);

    private void TryEnter(Collider other)
    {
        Rigidbody body = other.attachedRigidbody;
        if (body != null && body.TryGetComponent(out SlingAbility sling))
        {
            sling.TryEnter(this);
        }
    }
}
