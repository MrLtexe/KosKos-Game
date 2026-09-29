using System;
using System.Collections.Generic;
using UnityEngine;

// Bölüm sonunda arayüze giden özet
public struct RunSummary
{
    public float time;
    public int deaths;
    public int kumas;
    public int kumasTotal;
    public int belge;
    public int belgeTotal;
    public Medal medal;
    public RunResult result;
    // Katalogda olmayan bölümün sonucu kaydedilmez
    public bool saved;
    // Sonraki bölümün katalog sırası; yoksa -1
    public int nextLevelIndex;
    public LevelCatalog catalog;
}

// Bir bölüm oynanışı (A.2-A.4): süre, ölüm sayacı, toplanabilirler ve bölüm sonu.
// Toplanabilirler önce "elde"dir; güvenli alana ulaşınca "güvenceye" alınır, ölünce elde olanlar geri gelir.
// Güvenceye alınanlar bölüm bitince kaydedilir. LevelSettings ile aynı nesnede durur.
[RequireComponent(typeof(LevelSettings))]
public class LevelRun : MonoBehaviour
{
    public static LevelRun Current { get; private set; }

    public event Action<RunSummary> Finished;

    // Gerçek zamanlı süre (sapan yavaşlaması sayacı yavaşlatmaz, duraklatma durdurur)
    public float Elapsed { get; private set; }
    public int Deaths { get; private set; }
    public bool IsFinished { get; private set; }
    public bool Paused { get; set; }

    public int KumasTotal { get; private set; }
    public int BelgeTotal { get; private set; }
    public int KumasCount => CountOf(Collectible.Kind.Kumas);
    public int BelgeCount => CountOf(Collectible.Kind.Belge);

    private LevelSettings settings;
    private PlayerDeath playerDeath;
    private bool saveEnabled;
    private Collectible[] collectibles;
    // Elde olan (henüz güvenceye alınmamış) ve güvenceye alınmış toplanabilirler
    private readonly List<Collectible> held = new List<Collectible>();
    private readonly List<Collectible> secured = new List<Collectible>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Current = null;
    }

    private void Awake()
    {
        Current = this;
        settings = GetComponent<LevelSettings>();

        saveEnabled = settings.Catalog != null && settings.Catalog.IndexOf(settings.LevelId) >= 0;
        if (!saveEnabled)
        {
            Debug.LogWarning($"[KosKos] '{settings.LevelId}' bölüm kataloğunda yok: sonuç kaydedilmeyecek.");
        }

        collectibles = FindObjectsByType<Collectible>(FindObjectsSortMode.None);
        foreach (Collectible c in collectibles)
        {
            if (c.Type == Collectible.Kind.Kumas) KumasTotal++;
            else BelgeTotal++;
        }
    }

    private void Start()
    {
        // Start'ta: toplanabilirlerin Awake'i (malzeme kopyası) çalışmış olsun
        if (saveEnabled)
        {
            foreach (Collectible c in collectibles) c.SetOwned(GameProgress.IsCollected(settings.LevelId, c.Id));
        }

        playerDeath = FindFirstObjectByType<PlayerDeath>();
        if (playerDeath != null)
        {
            playerDeath.Died += OnPlayerDied;
            // GEÇİCİ kostüm: giyili kostümün rengi
            if (playerDeath.TryGetComponent(out ChargeTintPlaceholder tint))
            {
                tint.SetBaseColor(GameProgress.EquippedColor);
                Debug.Log($"[KosKos] Kostüm: {GameProgress.EquippedCostumeId}");
            }
            else
            {
                Debug.LogWarning("[KosKos] Oyuncuda ChargeTintPlaceholder yok: kostüm rengi uygulanamadı.");
            }
        }
        SafeZone.Activated += OnSafeZoneActivated;
    }

    private void OnDestroy()
    {
        if (playerDeath != null) playerDeath.Died -= OnPlayerDied;
        SafeZone.Activated -= OnSafeZoneActivated;
        if (Current == this) Current = null;
    }

    private void Update()
    {
        if (!IsFinished && !Paused) Elapsed += Time.unscaledDeltaTime;
    }

    public void OnCollected(Collectible collectible)
    {
        if (IsFinished || held.Contains(collectible) || secured.Contains(collectible)) return;
        held.Add(collectible);
    }

    private void OnSafeZoneActivated(SafeZone _)
    {
        SecureHeld();
    }

    private void OnPlayerDied()
    {
        if (IsFinished) return;
        Deaths++;
        // Güvenli alana ulaşmadan ölününce elde olanlar geri gelir
        foreach (Collectible c in held) c.Reappear();
        held.Clear();
    }

    private void SecureHeld()
    {
        secured.AddRange(held);
        held.Clear();
    }

    public void Finish()
    {
        if (IsFinished) return;
        IsFinished = true;
        // Bölüm sonu da güvenli alan gibi elde olanları güvenceye alır
        SecureHeld();

        var summary = new RunSummary
        {
            time = Elapsed,
            deaths = Deaths,
            kumas = KumasCount,
            kumasTotal = KumasTotal,
            belge = BelgeCount,
            belgeTotal = BelgeTotal,
            medal = MedalRule.Compute(Elapsed, AllCollected(), settings.SilverTime, settings.GoldTime),
            saved = saveEnabled,
            nextLevelIndex = -1,
            catalog = settings.Catalog
        };

        if (saveEnabled)
        {
            var ids = new List<string>(secured.Count);
            foreach (Collectible c in secured) ids.Add(c.Id);
            summary.result = GameProgress.SubmitRun(settings.LevelId, Elapsed, Deaths, ids, summary.medal, 1);

            int next = settings.Catalog.IndexOf(settings.LevelId) + 1;
            if (next < settings.Catalog.Entries.Count) summary.nextLevelIndex = next;
        }

        // Sonuç ekranının arkasında hiçbir şey hareket etmesin (düşman mermileri dahil)
        Time.timeScale = 0f;
        Finished?.Invoke(summary);
    }

    // Altın için: bölümdeki her toplanabilir BU oynanışta toplanmış olmalı (önceki oynanışlarda toplananlar sayılmaz;
    // soluk görünenler tekrar alınınca sayılır)
    private bool AllCollected()
    {
        foreach (Collectible c in collectibles)
        {
            if (!secured.Contains(c)) return false;
        }
        return true;
    }

    private int CountOf(Collectible.Kind kind)
    {
        int count = 0;
        foreach (Collectible c in held) if (c.Type == kind) count++;
        foreach (Collectible c in secured) if (c.Type == kind) count++;
        return count;
    }
}
