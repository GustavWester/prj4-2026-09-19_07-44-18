using UnityEngine;

// kører før alle andre scripts, så en dublet (fx NetworkManager) slås fra inden dens egen Awake
[DefaultExecutionOrder(-10000)]
public class DontDestroy : MonoBehaviour
{
    private void Awake()
    {
        // Try again genindlæser DungeonScene, og så findes den gamle kopi allerede i DontDestroyOnLoad
        foreach (var other in FindObjectsByType<DontDestroy>())
        {
            if (other == this || other.name != name || other.gameObject.scene.name != "DontDestroyOnLoad") continue;
            gameObject.SetActive(false); // de andre komponenter på dubletten når ikke at køre Awake
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
    }
}
