using TMPro;
using UnityEngine;

/// <summary>
/// Lille tal der popper op og svæver væk, fx "+25" når man healer eller "-10" når en fjende bliver ramt.
/// Laves fra kode: FloatingText.Spawn(position, "+25", Color.green). Ingen prefab: teksten bygges her,
/// med samme pixel-font som navneskiltet (UI/Resources/Fonts/m5x7).
/// </summary>
public class FloatingText : MonoBehaviour
{
    private const float Lifetime = 0.8f;
    private const float Rise = 0.6f; // world-enheder over hele levetiden
    private static TMP_FontAsset font;

    private TextMeshPro text;
    private Color color;
    private float age;

    public static void Spawn(Vector3 position, string message, Color color)
    {
        if (font == null) font = Resources.Load<TMP_FontAsset>("Fonts/m5x7");
        var go = new GameObject("FloatingText");
        go.transform.position = position + new Vector3(Random.Range(-0.15f, 0.15f), 0, 0); // flere hits lige efter hinanden overlapper ikke
        var tmp = go.AddComponent<TextMeshPro>();
        if (font != null) tmp.font = font;
        tmp.text = message;
        tmp.fontSize = 5; // samme som navneskiltet
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.sortingOrder = 100; // over wizards, fjender og navneskilte
        var popup = go.AddComponent<FloatingText>();
        popup.text = tmp;
        popup.color = color;
    }

    private void Update()
    {
        age += Time.deltaTime;
        float k = age / Lifetime;
        if (k >= 1f) { Destroy(gameObject); return; }
        transform.position += Vector3.up * (Rise / Lifetime * Time.deltaTime);
        text.color = new Color(color.r, color.g, color.b, 1f - k * k); // holder sig synlig og fader til sidst
    }
}
