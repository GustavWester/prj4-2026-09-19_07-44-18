using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Game over-sedlen. Prefab: UI/Prefabs/DeathScreenUI (bygges med Tools > Create Death Screen UI, kan rettes bagefter).
/// DeathScreen på wizarden laver den, når min wizard dør, og styrer knapperne.
/// </summary>
public class DeathScreenUI : MonoBehaviour
{
    public TMP_Text titleText;   // byttes ud på victory-skærmen (DeathScreen.ShowVictory)
    public TMP_Text messageText;
    public TMP_Text killedByText;
    public Button tryAgainButton;
    public Button waitForReviveButton;
}
