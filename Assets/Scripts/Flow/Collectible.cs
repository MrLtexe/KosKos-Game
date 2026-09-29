using UnityEngine;

// Toplanabilir (A.3): Belge (hikâye) veya Kumaş (mağaza parası). Toplamak zorunlu değildir.
// Dokununca kaybolur ve LevelRun'a bildirir. Bir sonraki güvenli alana ulaşmadan ölünürse geri gelir.
// Daha önceki bir oynanışta kaydedilmişse soluk görünür; yine alınabilir ama tekrar sayılmaz.
[RequireComponent(typeof(Collider))]
public class Collectible : MonoBehaviour
{
    public enum Kind { Belge, Kumas }

    [Tooltip("Toplanabilirin türü.")]
    [SerializeField] private Kind kind;

    [Tooltip("Kalıcı kimlik (ör. L1_Kumas_3). Kayıt dosyası bunu kullanır; bölüm içinde benzersiz olmalı ve sonradan değiştirilmemeli.")]
    [SerializeField] private string id;

    [Tooltip("GEÇİCİ görselin rengi.")]
    [SerializeField] private Color color = Color.white;

    [Tooltip("Dönme hızı (derece/sn). 0 = dönmez.")]
    [SerializeField] private float spinSpeed;

    // Daha önce toplanmış olanın rengi normal renge göre ne kadar griye kayar (0-1)
    private const float OwnedFade = 0.7f;
    private static readonly Color OwnedGrey = new Color(0.35f, 0.35f, 0.35f);

    public Kind Type => kind;
    public string Id => id;

    private Collider[] colliders;
    private Renderer[] renderers;
    private Material material;
    private bool taken;

    private void Awake()
    {
        colliders = GetComponentsInChildren<Collider>();
        renderers = GetComponentsInChildren<Renderer>();
        Renderer main = GetComponent<Renderer>();
        // .material kopya oluşturur; diğer nesneler etkilenmez
        if (main != null)
        {
            material = main.material;
            material.color = color;
        }
    }

    private void OnDestroy()
    {
        if (material != null) Destroy(material);
    }

    private void Update()
    {
        if (spinSpeed != 0f && !taken) transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
    }

    // LevelRun bölüm başında çağırır: daha önce kaydedildiyse soluk göster
    public void SetOwned(bool owned)
    {
        if (material != null) material.color = owned ? Color.Lerp(color, OwnedGrey, OwnedFade) : color;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (taken) return;
        Rigidbody body = other.attachedRigidbody;
        if (body == null || !body.TryGetComponent(out PlayerDeath death) || death.IsDead) return;

        taken = true;
        SetPresent(false);
        if (LevelRun.Current != null) LevelRun.Current.OnCollected(this);
    }

    // Güvenli alana ulaşmadan ölününce toplanabilir geri gelir
    public void Reappear()
    {
        taken = false;
        SetPresent(true);
    }

    private void SetPresent(bool present)
    {
        foreach (Collider c in colliders) c.enabled = present;
        foreach (Renderer r in renderers) r.enabled = present;
    }
}
