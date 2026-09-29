using UnityEngine;

// Bir bölümde oyuncunun sahip olduğu yükseltmeler ve aletler.
// Her bölüm için bir asset oluşturulur: Assets > Create > KosKos > Ability Loadout.
// Bölüm sahnesindeki LevelSettings bu asset'i gösterir; oyuncu bölüm başında buna göre ayarlanır.
[CreateAssetMenu(fileName = "Loadout", menuName = "KosKos/Ability Loadout")]
public class AbilityLoadout : ScriptableObject
{
    [Header("Temel Mekanik Yükseltmeleri")]
    [Tooltip("Çift zıplama (B.2.1): havada bir kez daha zıplama.")]
    [SerializeField] private bool doubleJump;

    [Tooltip("Sıyrılganlık (B.3.1): atılırken ince duvarların (Phaseable) ve dron mermilerinin içinden geçme.")]
    [SerializeField] private bool phase;

    [Tooltip("Savuşturma (B.5.1): tavan silahı mermilerini kılıçla sahibine geri gönderme.")]
    [SerializeField] private bool parry;

    [Header("Aletler")]
    [Tooltip("Kanca (C): E basılı tutunca öndeki kanca noktasına tutunup sallanma veya çekilme.")]
    [SerializeField] private bool hook;

    [Tooltip("Jet-Çanta (C): W basılı tutunca yukarı itki; yakıt sınırlı, yerde koşunca dolar.")]
    [SerializeField] private bool jetBag;

    [Tooltip("Manyetik Botlar (C): Q ile metal tavanda baş aşağı koşma ve metal duvarda duvar koşusu.")]
    [SerializeField] private bool magBoots;

    public bool DoubleJump => doubleJump;
    public bool Phase => phase;
    public bool Parry => parry;
    public bool Hook => hook;
    public bool JetBag => jetBag;
    public bool MagBoots => magBoots;
}
