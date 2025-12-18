using System.Collections.Generic;
using UnityEngine;

public class RecorderFriendlySwitcher : MonoBehaviour
{
    [Header("Setup")]
    public List<Camera> cameras;
    public KeyCode switchKey = KeyCode.C;
    
    [Header("Recording Settings")]
    [Tooltip("Locks FPS to 60 to stop Streamlabs lag")]
    public int recordingFPS = 60; 

    private int currentCamIndex = 0;

    void Start()
    {
        // 1. LOCK THE FPS
        // This prevents Unity from eating 100% GPU, leaving room for Streamlabs
        Application.targetFrameRate = recordingFPS;
        QualitySettings.vSyncCount = 0; // VSync must be off for targetFrameRate to work

        if (cameras.Count == 0) return;

        // 2. Prepare Cameras (Disable components, keep GameObjects active)
        for (int i = 0; i < cameras.Count; i++)
        {
            cameras[i].gameObject.SetActive(true); // Keep Object ON
            DisableCameraComponents(i);
        }

        // 3. Enable first camera
        EnableCameraComponents(0);
    }

    // void Update()
    // {
    //     if (Input.GetKeyDown(switchKey))
    //     {
    //         SwitchCamera();
    //     }
    // }

    void SwitchCamera()
    {
        if (cameras.Count == 0) return;

        // Turn off current
        DisableCameraComponents(currentCamIndex);

        // Next index
        currentCamIndex++;
        if (currentCamIndex >= cameras.Count) currentCamIndex = 0;

        // Turn on new
        EnableCameraComponents(currentCamIndex);
    }

    // Helper to turn on Camera + Audio
    void EnableCameraComponents(int index)
    {
        cameras[index].enabled = true;
        var audio = cameras[index].GetComponent<AudioListener>();
        if (audio != null) audio.enabled = true;
    }

    // Helper to turn off Camera + Audio
    void DisableCameraComponents(int index)
    {
        cameras[index].enabled = false;
        var audio = cameras[index].GetComponent<AudioListener>();
        if (audio != null) audio.enabled = false;
    }
}