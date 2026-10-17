using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Lyser rummet op, når min egen spiller står i det. Resten af banen er mørk (Global Light 2D intensity = 0).
/// Skal sidde på samme objekt som en Light 2D (rummets form) og en Box Collider 2D med Is Trigger (rummets gulv).
/// Lys er lokalt, så online ser hver spiller kun sit eget rum.
/// </summary>
[RequireComponent(typeof(Light2D), typeof(Collider2D))]
public class RoomVisibility : MonoBehaviour
{
    [Tooltip("Valgfrit ikon på minimappet, fx et kranie i boss-rummet eller en kiste.")]
    [SerializeField] private Sprite minimapIcon;
    public Sprite MinimapIcon => minimapIcon;

    private Light2D roomLight;

    private void Awake()
    {
        // tændt fra start, så rummet ses bag menuen. Slukkes når min spiller spawner (PlayerMovementController)
        roomLight = GetComponent<Light2D>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsMe(other)) return;
        roomLight.enabled = true;
        MinimapUI.Enter(this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (IsMe(other)) roomLight.enabled = false;
    }

    /// <summary>Rummets område (Box Collider 2D) der dækker punktet, eller null hvis punktet ikke er i noget rum.</summary>
    public static Collider2D RoomAt(Vector2 position)
    {
        foreach (var room in FindObjectsByType<RoomVisibility>())
        {
            var area = room.GetComponent<Collider2D>();
            if (area.OverlapPoint(position)) return area;
        }
        return null;
    }

    // kun min egen wizard tæller, ikke de andre spillere eller fjender
    private static bool IsMe(Collider2D other)
    {
        var player = other.GetComponent<PlayerMovementController>();
        return player != null && player.HasControl;
    }
}
