using UnityEngine;

// Metal duvar (C): arka plandaki metal duvarın oyun düzlemindeki alanı (trigger).
// Oyuncu havadayken bu alanın içinde Q'ya basarsa duvar koşusu başlar; alandan çıkınca biter.
[RequireComponent(typeof(Collider))]
public class MetalWall : MonoBehaviour
{
    private void OnTriggerEnter(Collider other) => Notify(other, true);
    private void OnTriggerExit(Collider other) => Notify(other, false);

    private void Notify(Collider other, bool inside)
    {
        Rigidbody body = other.attachedRigidbody;
        if (body != null && body.TryGetComponent(out MagBootsAbility boots))
        {
            boots.SetInsideWall(this, inside);
        }
    }
}
