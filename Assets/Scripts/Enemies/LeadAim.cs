using UnityEngine;

// Hedef önü nişan: oyuncunun şu anki hızıyla devam edeceğini varsayıp merminin onunla buluşacağı yöne nişan alır.
public class LeadAim : MonoBehaviour
{
    [Tooltip("Hedef önü miktarı. 1 = tam öngörü, 0 = oyuncunun şu anki konumuna nişan.")]
    [Range(0f, 1f)]
    [SerializeField] private float leadAmount = 1f;

    // Çok küçük sayıları sıfır kabul etmek için
    private const float Epsilon = 0.0001f;

    public Vector3 GetAimDirection(Vector3 muzzle, Vector3 targetPosition, Vector3 targetVelocity, float bulletSpeed)
    {
        Vector3 toTarget = targetPosition - muzzle;
        toTarget.z = 0f;
        targetVelocity.z = 0f;
        Vector3 direct = toTarget.normalized;

        // |toTarget + v*t| = bulletSpeed*t denkleminin en küçük pozitif çözümü
        float a = Vector3.Dot(targetVelocity, targetVelocity) - bulletSpeed * bulletSpeed;
        float b = 2f * Vector3.Dot(toTarget, targetVelocity);
        float c = Vector3.Dot(toTarget, toTarget);

        float t = -1f;
        if (Mathf.Abs(a) < Epsilon)
        {
            if (Mathf.Abs(b) > Epsilon) t = -c / b;
        }
        else
        {
            float discriminant = b * b - 4f * a * c;
            if (discriminant >= 0f)
            {
                float root = Mathf.Sqrt(discriminant);
                float t1 = (-b - root) / (2f * a);
                float t2 = (-b + root) / (2f * a);
                t = SmallestPositive(t1, t2);
            }
        }

        // Çözüm yoksa (mermi yetişemiyor) doğrudan oyuncuya nişan al
        if (t <= 0f) return direct;

        Vector3 intercept = (toTarget + targetVelocity * t).normalized;
        return Vector3.Lerp(direct, intercept, leadAmount).normalized;
    }

    private static float SmallestPositive(float x, float y)
    {
        if (x > 0f && y > 0f) return Mathf.Min(x, y);
        if (x > 0f) return x;
        if (y > 0f) return y;
        return -1f;
    }
}
