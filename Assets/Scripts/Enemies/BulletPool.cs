using System.Collections.Generic;
using UnityEngine;

// Mermileri yok edip yeniden yaratmak yerine tekrar kullanır (mobil için bellek dostu).
// Her EnemyShooter kendi havuzuna sahiptir.
public class BulletPool
{
    private readonly Bullet prefab;
    private readonly Transform container;
    private readonly List<Bullet> bullets = new List<Bullet>();

    public BulletPool(Bullet prefab, Transform container, int prewarmCount)
    {
        this.prefab = prefab;
        this.container = container;
        for (int i = 0; i < prewarmCount; i++) Create();
    }

    public Bullet Get()
    {
        foreach (Bullet b in bullets)
        {
            if (!b.gameObject.activeSelf) return b;
        }
        // Hepsi kullanımda: havuzu büyüt
        return Create();
    }

    public void Return(Bullet bullet)
    {
        bullet.gameObject.SetActive(false);
    }

    // Oyuncu ölünce ekrandaki tüm mermiler kaldırılır
    public void ReturnAll()
    {
        foreach (Bullet b in bullets)
        {
            if (b.gameObject.activeSelf) b.gameObject.SetActive(false);
        }
    }

    private Bullet Create()
    {
        Bullet b = Object.Instantiate(prefab, container);
        b.gameObject.SetActive(false);
        bullets.Add(b);
        return b;
    }
}
