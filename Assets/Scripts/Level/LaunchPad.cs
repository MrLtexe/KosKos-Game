using UnityEngine;

// Fırlatma alanı (D.5): oyuncu üstüne gelince otomatik olarak yukarı fırlatır ve bir ek hava zıplaması verir.
[RequireComponent(typeof(Collider))]
public class LaunchPad : MonoBehaviour
{
    [Tooltip("Oyuncunun fırlatılacağı yükseklik (birim).")]
    [SerializeField] private float launchHeight = 6f;

    private void OnTriggerEnter(Collider other)
    {
        Rigidbody body = other.attachedRigidbody;
        if (body == null || !body.TryGetComponent(out PlayerMotor motor)) return;
        if (!body.TryGetComponent(out PlayerDeath death) || !body.TryGetComponent(out JumpAbility jump)) return;

        // Ölüyken veya başka bir yetenek hareketi yönetirken (atılma, zipline, sapan) fırlatma yok
        if (death.IsDead || motor.IsControlLocked) return;

        motor.Jump(launchHeight);
        jump.GrantBonusAirJump();
    }
}
