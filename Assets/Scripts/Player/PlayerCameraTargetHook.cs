using Cinemachine;
using UnityEngine;

public class PlayerCameraTargetHook : MonoBehaviour
{
    private CinemachineVirtualCamera _vCam;

    void Awake()
    {
        _vCam = GetComponent<CinemachineVirtualCamera>();
    }

    void Start()
    {
        // Find the player object using its Tag
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            // Set the camera to follow and look at the player's transform
            _vCam.Follow = player.transform;
            //_vCam.LookAt = player.transform;
        }
        else
        {
            Debug.LogWarning("CameraTargetHook: Player not found in scene!");
        }
    }
}
