using UnityEngine;
using Unity.Cinemachine;
using System.Collections.Generic;

[System.Serializable]
public struct CinmachineKeyBind
{
    public KeyCode key;
    public CinemachineCamera camera;

    public CinmachineKeyBind(KeyCode key, CinemachineCamera camera)
    {
        this.key = key;
        this.camera = camera;
    }
}

public class FakeDirector : MonoBehaviour
{
    public List<CinmachineKeyBind> cameras;

    private CinemachineCamera currentCamera;

    public GameObject mainCharacter;

    void Start()
    {
        // Disable all cameras at start
        foreach (var cam in cameras)
        {
            if (cam.camera != null)
                cam.camera.gameObject.SetActive(false);
        }

        // Optional: activate the first camera as default
        if (cameras.Count > 0 && cameras[0].camera != null)
        {
            ActivateCamera(cameras[0].camera);
        }
    }

    void Update()
    {
        foreach (var bind in cameras)
        {
            if (Input.GetKeyDown(bind.key))
            {
                ActivateCamera(bind.camera);
                break;
            }
        }
    }

    private void ActivateCamera(CinemachineCamera cam)
    {
        if (cam == null || cam == currentCamera)
            return;

        // Disable previous camera
        if (currentCamera != null)
            currentCamera.gameObject.SetActive(false);

        // Enable new camera
        cam.gameObject.SetActive(true);
        cam.Target.TrackingTarget = mainCharacter.transform;
        currentCamera = cam;
    }
}
