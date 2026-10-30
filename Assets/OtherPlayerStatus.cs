using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Viser navn, liv og mana på de andre spillere i en horizontal række.
/// Opretter ét OtherPlayerObject pr. spiller der joiner, og fjerner det når de går.
///
/// Prefab-struktur (navnene skal passe):
///   OtherPlayerObject
///   ├ Image            (baggrund)
///   ├ portrait_0
///   ├ Text (TMP)       (navn)
///   ├ HealthBar        (Image, Type = Filled, Horizontal)
///   └ ManaBar          (Image, Type = Filled, Horizontal)
/// </summary>
public class OtherPlayerStatus : MonoBehaviour
{
    [SerializeField] private GameObject entryPrefab;
    [Tooltip("Objekt med HorizontalLayoutGroup. Tom = dette objekt.")]
    [SerializeField] private Transform container;
    [SerializeField] private float refreshInterval = 0.5f;

    [Header("Farver")]
    [SerializeField] private Color healthColor = new Color(0.35f, 0.8f, 0.35f);
    [SerializeField] private Color lowHealthColor = new Color(0.9f, 0.1f, 0.1f);
    [SerializeField] private Color manaColor = new Color(0.3f, 0.6f, 1f);

    [Header("Lavt liv")]
    [Range(0f, 1f)]
    [SerializeField] private float lowHealthThreshold = 0.3f;
    [Tooltip("Pulser pr. sekund i radianer. 2.5 = en puls ca. hvert 2,5 sekund.")]
    [SerializeField] private float pulseSpeed = 2.5f;

    private class Row
    {
        public GameObject player;
        public GameObject ui;
        public ResourceController resources;
        public TMP_Text nameText;
        public Image healthFill;
        public Image manaFill;
    }

    private readonly List<Row> rows = new();

    private void OnEnable() => InvokeRepeating(nameof(DisplayPlayers), 0f, refreshInterval);
    private void OnDisable() => CancelInvoke(nameof(DisplayPlayers));

    private void Update()
    {
        foreach (var row in rows)
        {
            if (row.resources == null) continue;

            // Navnet opdateres løbende, så det også virker hvis det først bliver sat efter spawn
            string playerName = GetPlayerName(row.player);
            if (row.nameText.text != playerName) row.nameText.text = playerName;

            float hp = row.resources.HealthPercent;
            row.healthFill.fillAmount = hp;
            row.manaFill.fillAmount = row.resources.ManaPercent;

            // Lavt liv: baren pulserer langsomt rød
            if (hp > 0f && hp <= lowHealthThreshold)
            {
                float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
                row.healthFill.color = Color.Lerp(healthColor, lowHealthColor, t);
            }
            else
            {
                row.healthFill.color = healthColor;
            }
        }
    }

    public void DisplayPlayers()
    {
        var players = new List<GameObject>();
        foreach (GameObject p in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (p == LocalPlayer.Current) continue; // kun "de andre"
            if (p.GetComponent<ResourceController>() == null) continue;
            players.Add(p);
        }

        if (SameAsCurrent(players)) return; // intet er ændret

        foreach (var row in rows) Destroy(row.ui);
        rows.Clear();

        Transform parent = container != null ? container : transform;

        foreach (GameObject p in players)
        {
            var ui = Instantiate(entryPrefab, parent);

            var nameText = ui.GetComponentInChildren<TMP_Text>();
            var healthFill = FindImage(ui.transform, "HealthBar");
            var manaFill = FindImage(ui.transform, "ManaBar");

            if (nameText == null || healthFill == null || manaFill == null)
            {
                Debug.LogError("OtherPlayerStatus: prefab mangler TMP-tekst, HealthBar (Image) eller ManaBar (Image).", this);
                Destroy(ui);
                continue;
            }

            manaFill.color = manaColor;

            rows.Add(new Row
            {
                player = p,
                ui = ui,
                resources = p.GetComponent<ResourceController>(),
                nameText = nameText,
                healthFill = healthFill,
                manaFill = manaFill
            });
        }
    }

    private bool SameAsCurrent(List<GameObject> players)
    {
        if (players.Count != rows.Count) return false;
        foreach (var row in rows)
            if (row.player == null || !players.Contains(row.player)) return false;
        return true;
    }

    private static Image FindImage(Transform root, string path)
    {
        var t = root.Find(path);
        return t != null ? t.GetComponent<Image>() : null;
    }

    /// <summary>
    /// Her skal spillerens rigtige navn hentes, når din ven har lavet det.
    /// Midlertidigt: GameObject-navnet uden "(Clone)".
    /// </summary>
    private static string GetPlayerName(GameObject player)
    {
        return player.name.Replace("(Clone)", "").Trim();
    }
}