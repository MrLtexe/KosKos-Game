using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using U = TestSceneBuilderUtils;

// Tek tıkla düşman + savuşturma test sahnesini üretir: KosKos > Build Section D Enemies Test Scene.
// Sahne her seferinde yeniden oluşturulur; mermi prefab'ları sadece yoksa oluşturulur.
public static class SectionDEnemiesTestSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Test_SectionD_Enemies.unity";
    private const string BulletFolder = "Assets/Prefabs/Bullets";
    private const string TurretBulletPath = BulletFolder + "/TurretBullet.prefab";
    private const string DroneBulletPath = BulletFolder + "/DroneBullet.prefab";

    private static readonly Color TurretColor = new Color(0.85f, 0.15f, 0.15f);
    private static readonly Color DroneColor = new Color(0.9f, 0.2f, 0.9f);
    private static readonly Color TurretBulletColor = new Color(1f, 0.9f, 0.1f);
    private static readonly Color DroneBulletColor = new Color(0.55f, 0.2f, 1f);

    // Diğer builder'lar (prototip bölümler) için hazır mermi prefab'ları
    internal static Bullet TurretBullet(U.Layers layers) =>
        GetOrCreateBulletPrefab(TurretBulletPath, layers, true, Bullet.DodgeRule.AnyDash, TurretBulletColor);

    internal static Bullet DroneBullet(U.Layers layers) =>
        GetOrCreateBulletPrefab(DroneBulletPath, layers, false, Bullet.DodgeRule.PhaseDash, DroneBulletColor);

    [MenuItem("KosKos/Build Section D Enemies Test Scene")]
    private static void Build()
    {
        if (!U.BeginScene(out Scene scene, out InputActionAsset actions, out U.Layers layers)) return;

        Bullet turretBullet = TurretBullet(layers);
        Bullet droneBullet = DroneBullet(layers);

        Transform level = new GameObject("Level").transform;
        U.Ground(level, layers.Ground, "Ground", -5f, 200f);
        U.SafeZoneAt(level, "SafeZone_Start", 0f, true);
        U.SafeZoneAt(level, "SafeZone_Parry", 75f, false);
        U.SafeZoneAt(level, "SafeZone_Drones", 135f, false);
        U.KillZone(level, 100f, 300f);

        // Tavan silahları: hedef önü atış; kaçış (atılma/zıplama) ve savuşturma denemesi
        Turret(level, layers, "Turret_Dodge", 30f, turretBullet);
        Turret(level, layers, "Turret_Parry", 60f, turretBullet);

        // Dronlar: alçak (kılıçla öldür), yüksek (havada vur → atılma yenilenir), yüksek (sıyrılganlık ile kaç)
        Drone(level, layers, "Drone_Low", 100f, 1.5f, droneBullet, 5f);
        Drone(level, layers, "Drone_High", 120f, 4f, droneBullet, 5f);
        // Uzun menzil: mermiler oyuncuya önden gelir, böylece atılma ile içlerinden geçmek denenebilir
        Drone(level, layers, "Drone_Phase", 160f, 4.5f, droneBullet, 12f);

        GameObject player = U.SpawnPlayer(layers, actions, new Vector3(0f, 1.05f, 0f));
        U.AddFollowCamera(player);

        U.SaveScene(scene, ScenePath);
    }

    internal static void Turret(Transform parent, U.Layers layers, string name, float x, Bullet bulletPrefab)
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

        SetupEnemy(turret, bulletPrefab, false, new Vector3(0f, -0.6f, 0f),
            18f, 0.6f, 1.5f, 1, 0f, 14f);
    }

    internal static void Drone(Transform parent, U.Layers layers, string name, float x, float y, Bullet bulletPrefab, float range)
    {
        GameObject drone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        drone.name = name;
        drone.layer = layers.Enemy;
        drone.transform.SetParent(parent);
        drone.transform.position = new Vector3(x, y, 0f);
        drone.GetComponent<Collider>().isTrigger = true;
        U.Colorize(drone, DroneColor);

        SetupEnemy(drone, bulletPrefab, true, Vector3.zero,
            range, 0.4f, 2f, 3, 8f, 10f);
    }

    private static void SetupEnemy(GameObject enemy, Bullet bulletPrefab, bool swordCanKill, Vector3 muzzleLocal,
        float range, float windUp, float cooldown, int bulletsPerShot, float spread, float bulletSpeed)
    {
        var body = enemy.AddComponent<EnemyBody>();
        U.SetBool(body, "swordCanKill", swordCanKill);
        enemy.AddComponent<LeadAim>();

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
