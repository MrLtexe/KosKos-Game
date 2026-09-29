// Kılıçla vurulabilen her şey (kırılabilirler, düşmanlar, mermiler).
// Dönüş değeri: bu vuruş "gerçek isabet" sayıldı mı? (havada atılmayı sadece gerçek isabet yeniler)
public interface ISwordHittable
{
    bool OnSwordHit(SwordAttack source);
}
