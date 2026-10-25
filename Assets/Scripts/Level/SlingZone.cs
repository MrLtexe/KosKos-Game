using UnityEngine;

// Sapan alanı (D.7): oyuncu havadayken fiziksel olarak girince SlingAbility devreye girer.
[RequireComponent(typeof(Collider))]
public class SlingZone : MonoBehaviour
{
    [Tooltip("İşaretliyse bu alanda oyuncuya nasıl kullanılacağı (fareyle nişan + fırlatma tuşu) uzun ipucuyla anlatılır. Bölümdeki ilk sapan alanı için.")]
    [SerializeField] private bool showTutorialHint;

    public bool ShowTutorialHint => showTutorialHint;

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
