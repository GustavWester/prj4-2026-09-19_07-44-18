using UnityEngine;

public class CameraController : MonoBehaviour
{
    public GameObject player;
    private Vector3 offset = new Vector3(0, 0, - 10);

    private CameraShake shake;

    void Start()
    {
        // fireballs ryster kameraet (PlayerBullet). Lægges på her, så alle scener får det uden at rette kameraet
        if (!TryGetComponent(out shake)) shake = gameObject.AddComponent<CameraShake>();

        // før man har joinet: vis rummet hvor spillerne starter, så menuen har banen som baggrund
        var spawn = GameObject.Find("PlayerSpawn");
        if (spawn != null) transform.position = spawn.transform.position + offset;
    }

    void LateUpdate()
    {
        if (player == null) return; // ingen spiller før man har joinet
        transform.position = player.transform.position + offset + shake.Offset; //henter positionen af player og sætter det til vores camera position

    }
}