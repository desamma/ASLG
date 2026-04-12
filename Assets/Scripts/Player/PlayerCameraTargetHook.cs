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
            // Dự phòng: Nếu Prefab quên gắn Tag "Player", tìm qua Component
            PlayerMovement pm = FindObjectOfType<PlayerMovement>();
            if (pm != null)
            {
                _vCam.Follow = pm.transform;
            }
        }
    }

    // Hàm public cho phép gán mục tiêu trực tiếp từ PlayerSpawner
    public void SetTarget(Transform target)
    {
        if (_vCam == null) _vCam = GetComponent<CinemachineVirtualCamera>();
        if (_vCam != null) _vCam.Follow = target;
    }
}
