using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Healthbar til fjender (World Space canvas på prefabben) og bossen (HUD-canvas).
/// Læser bare ResourceController.HealthPercent. Livet er en NetworkVariable, så alle klienter ser det samme.
/// Poller hver frame i stedet for onHealthPercentChanged, fordi eventet ikke fyrer på startværdien
/// (en klient der joiner sent ville ellers se en fuld bar).
/// </summary>
public class HealthBar : MonoBehaviour
{
    [Tooltip("Tom = ResourceController på forælderen (fjende-prefab). Boss-bar: træk bossen fra scenen ind.")]
    [SerializeField] private ResourceController target;
    [Tooltip("Image med Image Type = Filled, Fill Method = Horizontal.")]
    [SerializeField] private Image fill;
    [Tooltip("Det der skjules. Må ikke være dette objekt, ellers stopper Update.")]
    [SerializeField] private GameObject visuals;
    [Tooltip("Boss-bar: vis kun når min spiller står i bossens rum.")]
    [SerializeField] private bool onlyInBossRoom;
    [Tooltip("Valgfri. Får enemyName fra target'ens EnemyStatsSO, så hver boss viser sit eget navn.")]
    [SerializeField] private TMP_Text nameText;

    private Collider2D room;
    private Transform me;

    private void Start()
    {
        if (target == null) target = GetComponentInParent<ResourceController>();
        if (target == null) return;
        room = RoomVisibility.RoomAt(target.transform.position);

        var enemy = target.GetComponent<EnemyController>();
        if (nameText != null && enemy != null && enemy.stats != null)
            nameText.text = string.IsNullOrEmpty(enemy.stats.bossTitle) ? enemy.stats.enemyName : enemy.stats.bossTitle;
    }

    private void Update()
    {
        bool alive = target != null && target.CurrentHealth > 0;
        bool show = alive && (!onlyInBossRoom || MeInRoom());
        if (visuals != null && visuals.activeSelf != show) visuals.SetActive(show);
        if (show) fill.fillAmount = target.HealthPercent;
    }

    private bool MeInRoom()
    {
        if (room == null) return true; // bossen står ikke i et rum: vis altid
        if (me == null) me = FindMe();
        return me != null && room.OverlapPoint(me.position);
    }

    // min egen wizard, samme tjek som LevelExit/RoomVisibility
    private static Transform FindMe()
    {
        foreach (var p in FindObjectsByType<PlayerMovementController>())
            if (p.HasControl) return p.transform;
        return null;
    }
}
