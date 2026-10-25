using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Yapımcılar paneli: ekip üyeleri ve rolleri (roller seçili dilde).
// Satır şablonunda "Role" (solda, sağa yaslı) ve "Name" (sağda) adlı iki yazı olmalı; görünüm MainMenuUI prefab'ında düzenlenir.
// Ekibe kişi eklemek/rol değiştirmek için Team listesini düzenleyin; yeni rol için Loc tablosuna "role.*" anahtarı ekleyin.
public class CreditsView : MonoBehaviour
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

    [Tooltip("Yapımcı satırlarının listesi (şablonda 'Role' ve 'Name' adlı iki TextMeshPro yazısı olmalı).")]
    [SerializeField] private UiRowList rows;

    [Tooltip("Geri butonu (ana menüye döner).")]
    [SerializeField] private Button backButton;

    public Button BackButton => backButton;

    public Selectable Open()
    {
        rows.Clear();
        foreach (Member member in Team)
        {
            GameObject row = rows.Add();
            row.transform.Find("Role").GetComponent<TMP_Text>().text = RolesText(member);
            row.transform.Find("Name").GetComponent<TMP_Text>().text = member.Name;
        }
        return backButton;
    }

    private static string RolesText(Member member)
    {
        var roles = new string[member.RoleKeys.Length];
        for (int i = 0; i < roles.Length; i++) roles[i] = Loc.Get(member.RoleKeys[i]);
        return string.Join(", ", roles);
    }
}
