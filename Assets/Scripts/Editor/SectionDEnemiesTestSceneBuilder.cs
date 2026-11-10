using UnityEditor;
using UnityEngine;
using U = TestSceneBuilderUtils;

// Düşmanların (tavan silahı, dron; Section D) ortak sahne parçaları: test dünyası ve prototip bölümler kullanır.
// Mermi prefab'ları sadece yoksa oluşturulur.
// (Eski ayrı "Section D Enemies Test Scene" sahnesi kaldırıldı; testler KosKos > Build Test World ile yapılır.)
public static class SectionDEnemiesTestSceneBuilder
{
    private const string BulletFolder = "Assets/Prefabs/Bullets";
    private const string TurretBulletPath = BulletFolder + "/TurretBullet.prefab";
    private const string DroneBulletPath = BulletFolder + "/DroneBullet.prefab";

    private static readonly Color TurretColor = new Color(0.85f, 0.15f, 0.15f);
    private static readonly Color DroneColor = new Color(0.9f, 0.2f, 0.9f);
    private static readonly Color TurretBulletColor = new Color(1f, 0.9f, 0.1f);
    private static readonly Color DroneBulletColor = new Color(0.55f, 0.2f, 1f);

    // Dronun en düşük yüksekliği: süzülmede alçalsa bile yerdeki oyuncunun (tepesi y=2) üstünde kalır
    private const float MinDroneHeight = 4.5f;

    // Diğer builder'lar (prototip bölümler) için hazır mermi prefab'ları.
    // Tavan silahı mermisi: savuşturulur; sıradan atılma korumaz (iyi zamanlanırsa mermi arkada kalır), sıyrılganlıkla içinden geçilir
    internal static Bullet TurretBullet(U.Layers layers) =>
        GetOrCreateBulletPrefab(TurretBulletPath, layers, true, Bullet.DodgeRule.PhaseDash, TurretBulletColor);

    internal static Bullet DroneBullet(U.Layers layers) =>
        GetOrCreateBulletPrefab(DroneBulletPath, layers, false, Bullet.DodgeRule.PhaseDash, DroneBulletColor);

    // Tavan silahı: oyuncu soldan yaklaşırken (ekrana girdiği andan itibaren) tek tek ateş eder, altından geçilince susar.
    // Atış sayısı sınırsızdır; mesafe belirler. Bir atış = nişan (windUp) + ara (gap): varsayılanlarla ekrana girişten
    // altına varışa kadarki ~2.2 sn'de 3 atış olur. Yavaş değerler (ilk bölümler) daha az atış demektir
    internal static void Turret(Transform parent, U.Layers layers, string name, float x, Bullet bulletPrefab,
        float windUp = 0.45f, float aimLock = 0.15f, float bulletSpeed = 20f, float passTolerance = -2f,
        float gap = 0.25f, float range = 18f)
    {
        // Tavan bloğu (y 8-9) ve altına asılı silah; kılıç menzilinin dışında
        U.Block(parent, layers.Ground, name + "_Ceiling", x - 3f, 8f, 6f, 1f);

        GameObject turret = GameObject.CreatePrimitive(PrimitiveType.Cube);
        turret.name = name;
        turret.layer = layers.Enemy;
        turret.transform.SetParent(parent);
        turret.transform.position = new Vector3(x, 7.5f, 0f);
        turret.GetComponent<Collider>().isTrigger = true;
        U.Colorize(turret, TurretColor);

        // Menzil: varsayılan 18 = oyuncunun silahı ekranın sağ kenarında görmeye başladığı mesafe. Büyük = daha çok atış (zor)
        EnemyShooter shooter = SetupEnemy(turret, bulletPrefab, false, new Vector3(0f, -0.6f, 0f),
            range, windUp, gap, 1, 0f, bulletSpeed);
        U.SetFloat(shooter, "aimLockTime", aimLock);
        // Susma noktası: silahın x'i + passTolerance (varsayılan -2 = silahın 2 birim önü, altındaki sessiz alan).
        // Oyuncu oraya varınca susar; varmadan ateşleyemeyeceği nişana hiç başlamaz (arkadan / yarım atış yok)
        U.SetFloat(shooter, "passTolerance", passTolerance);
        U.SetBool(shooter, "cancelWhenPassed", true);
    }

    // Dronlar havadadır: yerdeki oyuncu altından her zaman geçebilir (en düşük yükseklik zorlanır).
    // patrolX: 0 = yerinde süzülür, aksi halde x ile x + patrolX arasında devriye gezer.
    internal static void Drone(Transform parent, U.Layers layers, string name, float x, float y, Bullet bulletPrefab, float range,
        float patrolX = 0f)
    {
        GameObject drone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        drone.name = name;
        drone.layer = layers.Enemy;
        drone.transform.SetParent(parent);
        drone.transform.position = new Vector3(x, Mathf.Max(y, MinDroneHeight), 0f);
        drone.GetComponent<Collider>().isTrigger = true;
        U.Colorize(drone, DroneColor);

        EnemyShooter shooter = SetupEnemy(drone, bulletPrefab, true, Vector3.zero,
            range, 0.4f, 2f, 3, 5f, 14f);
        U.SetFloat(shooter, "aimLockTime", 0.2f);

        var hover = drone.AddComponent<DroneHover>();
        if (patrolX != 0f)
        {
            var so = new SerializedObject(hover);
            so.FindProperty("patrolOffset").vector3Value = new Vector3(patrolX, 0f, 0f);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static EnemyShooter SetupEnemy(GameObject enemy, Bullet bulletPrefab, bool swordCanKill, Vector3 muzzleLocal,
        float range, float windUp, float cooldown, int bulletsPerShot, float spread, float bulletSpeed)
    {
        var body = enemy.AddComponent<EnemyBody>();
        U.SetBool(body, "swordCanKill", swordCanKill);

        var muzzle = new GameObject("Muzzle").transform;
        muzzle.SetParent(enemy.transform, false);
        muzzle.localPosition = muzzleLocal;

        var telegraphObject = new GameObject("AimTelegraph");
        telegraphObject.transform.SetParent(enemy.transform, false);
        var telegraph = telegraphObject.AddComponent<AimTelegraph>();

        var shooter = enemy.AddComponent<EnemyShooter>();
        U.SetFloat(shooter, "range", range);
        U.SetFloat(shooter, "windUpTime", windUp);
        U.SetFloat(shooter, "cooldownTime", cooldown);
        U.SetInt(shooter, "bulletsPerShot", bulletsPerShot);
        U.SetFloat(shooter, "spreadAngle", spread);
        U.SetFloat(shooter, "bulletSpeed", bulletSpeed);
        U.SetField(shooter, "bulletPrefab", bulletPrefab);
        U.SetField(shooter, "muzzle", muzzle);
        U.SetField(shooter, "telegraph", telegraph);
        return shooter;
    }

    // Mermi prefab'ı varsa onu kullanır (ayarlar korunur), yoksa oluşturur
    private static Bullet GetOrCreateBulletPrefab(string path, U.Layers layers, bool parryable, Bullet.DodgeRule dodgeRule, Color color)
    {
        var existing = AssetDatabase.LoadAssetAtPath<Bullet>(path);
        if (existing != null) return existing;

        U.EnsureFolder("Assets/Prefabs", "Bullets");

        // Küçük küre (yarıçap 0.2), trigger, kinematik
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = System.IO.Path.GetFileNameWithoutExtension(path);
        go.layer = layers.Enemy;
        go.transform.localScale = Vector3.one * 0.4f;
        go.GetComponent<Collider>().isTrigger = true;
        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        var bullet = go.AddComponent<Bullet>();
        U.SetBool(bullet, "parryable", parryable);
        U.SetEnum(bullet, "dodgeRule", (int)dodgeRule);
        U.SetLayerMask(bullet, "levelLayers", U.LevelMask(layers));
        U.Colorize(go, color);

        Bullet prefab = PrefabUtility.SaveAsPrefabAsset(go, path).GetComponent<Bullet>();
        Object.DestroyImmediate(go);
        Debug.Log($"[KosKos] Mermi prefab'ı oluşturuldu: {path}");
        return prefab;
    }
}
