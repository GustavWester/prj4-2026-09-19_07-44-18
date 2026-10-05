using UnityEngine;

public class CameraController : MonoBehaviour
{
    public GameObject player;
    private Vector3 offset = new Vector3(0, 0, - 10);

    void Start()
    {
        
    }

    void LateUpdate()
    {
        transform.position = player.transform.position + offset ; //henter positionen af player og sætter det til vores camera position
        if (player == null) return; // ingen spiller før man har joinet
    }
}