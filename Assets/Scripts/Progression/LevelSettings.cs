using UnityEngine;

// Bölüm ayarları: her bölüm sahnesinde bir tane bulunur.
// Bu bölümde hangi yeteneklerin açık olduğunu, bölümün katalogdaki kimliğini ve madalya sürelerini tutar.
public class LevelSettings : MonoBehaviour
{
    [Tooltip("Bu bölümde oyuncunun sahip olduğu yükseltmeler ve aletler.")]
    [SerializeField] private AbilityLoadout loadout;

    [Header("Bölüm Akışı")]
    [Tooltip("Bölüm listesi. Boşsa (ör. test sahneleri) bölüm sonucu kaydedilmez.")]
    [SerializeField] private LevelCatalog catalog;

    [Tooltip("Bu bölümün katalogdaki kimliği (ör. L1).")]
    [SerializeField] private string levelId;

    [Tooltip("Gümüş madalya için en uzun süre (sn). 0 = gümüş kapalı.")]
    [SerializeField] private float silverTime;

    [Tooltip("Altın madalya için en uzun süre (sn); ayrıca tüm toplanabilirler gerekir. 0 = altın kapalı.")]
    [SerializeField] private float goldTime;

    public AbilityLoadout Loadout => loadout;
    public LevelCatalog Catalog => catalog;
    public string LevelId => levelId;
    public float SilverTime => silverTime;
    public float GoldTime => goldTime;
}
