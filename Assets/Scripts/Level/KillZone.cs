using UnityEngine;

// Boşlukların altındaki görünmez alan: oyuncu girerse ölür.
[RequireComponent(typeof(Collider))]
public class KillZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Rigidbody body = other.attachedRigidbody;
        if (body != null && body.TryGetComponent(out PlayerDeath death))
        {
            death.Die();
        }
    }
}
