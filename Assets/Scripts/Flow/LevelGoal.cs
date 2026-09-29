using UnityEngine;

// Bölüm sonu (A.2): oyuncu buraya ulaşınca bölümü kazanmış sayılır.
[RequireComponent(typeof(Collider))]
public class LevelGoal : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Rigidbody body = other.attachedRigidbody;
        if (body == null || !body.TryGetComponent(out PlayerDeath death) || death.IsDead) return;

        // Finish birden fazla çağrılsa da LevelRun sadece ilkini işler
        if (LevelRun.Current != null) LevelRun.Current.Finish();
    }
}
