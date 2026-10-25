using System.Collections.Generic;
using UnityEngine;

// Satır listesi (bölüm seçimi, mağaza, yapımcılar, tuş ayarları): gizli bir şablon satırı her kayıt için kopyalar.
// Şablonu düzenlemek (boyut, renk, yazı tipi) tüm satırları değiştirir. Şablon bu nesnenin çocuğu olmalı ve kapalı durmalı.
public class UiRowList : MonoBehaviour
{
    [Tooltip("Kopyalanacak şablon satır (bu nesnenin kapalı bir çocuğu). Oyunda hiç görünmez, sadece kopyaları görünür.")]
    [SerializeField] private GameObject template;

    private readonly List<GameObject> rows = new List<GameObject>();

    private void Awake()
    {
        if (template != null) template.SetActive(false);
    }

    // Önceki kopyaları siler
    public void Clear()
    {
        foreach (GameObject row in rows)
        {
            if (row == null) continue;
            // Destroy kare sonunda çalışır; hemen gizlenir ki eski ve yeni satırlar bir kare bile birlikte görünmesin
            row.SetActive(false);
            Destroy(row);
        }
        rows.Clear();
    }

    // Şablonun açık bir kopyasını listenin sonuna ekler
    public GameObject Add()
    {
        GameObject row = Instantiate(template, template.transform.parent);
        row.name = template.name;
        row.SetActive(true);
        rows.Add(row);
        return row;
    }
}
