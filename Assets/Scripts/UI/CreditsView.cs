using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// GEÇİCİ yapımcılar ekranı: ekip üyeleri ve rolleri (roller seçili dilde).
// Düzen: başlık, altında her satırda solda rol (sağa yaslı, küçük, soluk), sağda isim (sola yaslı, büyük).
// Ekibe kişi eklemek/rol değiştirmek için Team listesini düzenleyin; yeni rol için Loc tablosuna "role.*" anahtarı ekleyin.
public static class CreditsView
{
    private readonly struct Member
    {
        public readonly string Name;
        public readonly string[] RoleKeys;

        public Member(string name, params string[] roleKeys)
        {
            Name = name;
            RoleKeys = roleKeys;
        }
    }

    private static readonly Member[] Team =
    {
        new Member("Mirza Çoban", "role.developer"),
        new Member("Yunus Yalçınkaya", "role.developer", "role.gameDesign"),
        new Member("Hasan Melih Özçelik", "role.artist3d"),
        new Member("Efe Civan", "role.artist3d"),
        new Member("Azra Azatoğlu", "role.artist2d", "role.narrativeDesigner"),
        new Member("Egecan Çınar", "role.director", "role.gameDesigner", "role.narrativeDesigner")
    };

    // Uzun rol listeleri tek satıra sığsın diye liste diğer ekranlardan geniş
    private const float ListWidth = 1500f;
    private const float RowHeight = 76f;
    private const float ColumnGap = 80f;
    private const int RoleSize = 28;
    private const int NameSize = 40;
    private static readonly Color RoleColor = new Color(0.8f, 0.82f, 0.85f);

    public static void Build(Transform parent, UnityAction onBack)
    {
        RectTransform list = UiKit.VerticalList(parent, 4f);
        list.sizeDelta = new Vector2(ListWidth, 0f);

        // Başlıktan sonra küçük bir boşluk
        UiKit.Line(list, Loc.Get("credits.title"), 56);
        UiKit.Line(list, "", 12);

        foreach (Member member in Team)
        {
            Row(list, RolesText(member), member.Name);
        }

        UiKit.Line(list, "", 12);
        Button back = UiKit.Button(list, Loc.Get("common.back"), onBack);
        UiKit.Select(back);
    }

    // Tek satır: solda rol, sağda isim; iki sütun eşit genişlikte, ortada boşluk
    private static void Row(Transform parent, string role, string name)
    {
        var go = new GameObject("CreditRow", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.AddComponent<LayoutElement>().preferredHeight = RowHeight;

        var layout = go.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = ColumnGap;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        Text roleLabel = UiKit.Label(go.transform, role, RoleSize, TextAnchor.MiddleRight);
        roleLabel.color = RoleColor;
        EqualColumn(roleLabel);
        EqualColumn(UiKit.Label(go.transform, name, NameSize, TextAnchor.MiddleLeft));
    }

    // Sütun genişliği yazının uzunluğuna göre değil, eşit paylaşılır (ortadaki boşluk hep ekranın ortasında kalır)
    private static void EqualColumn(Text label)
    {
        var element = label.gameObject.AddComponent<LayoutElement>();
        element.minWidth = 0f;
        element.preferredWidth = 0f;
        element.flexibleWidth = 1f;
    }

    private static string RolesText(Member member)
    {
        var roles = new string[member.RoleKeys.Length];
        for (int i = 0; i < roles.Length; i++) roles[i] = Loc.Get(member.RoleKeys[i]);
        return string.Join(", ", roles);
    }
}
