using UnityEngine;

// Bölümün AbilityLoadout'unu oyuncunun yeteneklerine uygular.
// Sahnede LevelSettings yoksa tüm yükseltmeler kapalı kalır (uyarı verilir).
[RequireComponent(typeof(JumpAbility), typeof(DashAbility), typeof(SwordAttack))]
[RequireComponent(typeof(HookAbility), typeof(JetBagAbility), typeof(MagBootsAbility))]
public class PlayerLoadout : MonoBehaviour
{
    private JumpAbility jump;
    private DashAbility dash;
    private SwordAttack sword;
    private HookAbility hook;
    private JetBagAbility jetBag;
    private MagBootsAbility magBoots;
    private AbilityLoadout loadout;

    public AbilityLoadout Current => loadout;

    private void Awake()
    {
        jump = GetComponent<JumpAbility>();
        dash = GetComponent<DashAbility>();
        sword = GetComponent<SwordAttack>();
        hook = GetComponent<HookAbility>();
        jetBag = GetComponent<JetBagAbility>();
        magBoots = GetComponent<MagBootsAbility>();

        LevelSettings settings = FindFirstObjectByType<LevelSettings>();
        loadout = settings != null ? settings.Loadout : null;
        if (loadout == null)
        {
            Debug.LogWarning("[KosKos] Sahnede LevelSettings veya atanmış bir AbilityLoadout yok: tüm yükseltmeler kapalı.");
        }

        Apply();
    }

#if UNITY_EDITOR
    // Sadece editörde: Play sırasında loadout asset'indeki tikler anında etki etsin (test kolaylığı)
    private void Update()
    {
        Apply();
    }
#endif

    private void Apply()
    {
        jump.DoubleJumpUnlocked = loadout != null && loadout.DoubleJump;
        dash.PhaseUnlocked = loadout != null && loadout.Phase;
        sword.ParryUnlocked = loadout != null && loadout.Parry;
        hook.Unlocked = loadout != null && loadout.Hook;
        jetBag.Unlocked = loadout != null && loadout.JetBag;
        magBoots.Unlocked = loadout != null && loadout.MagBoots;
    }
}
