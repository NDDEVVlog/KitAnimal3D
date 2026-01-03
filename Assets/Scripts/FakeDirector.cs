using UnityEngine;
using Unity.Cinemachine;
using System.Collections.Generic;


public enum TargetObject
{
    Player,
    Object
}
[System.Serializable]
public struct CinmachineKeyBind
{
    public KeyCode key;
    public CinemachineCamera camera;
    public TargetObject targetObject;

    public CinmachineKeyBind(KeyCode key, CinemachineCamera camera,TargetObject targetObject)
    {
        this.key = key;
        this.camera = camera;
        this.targetObject= targetObject;
    }
}

public class FakeDirector : MonoBehaviour
{   
    public static FakeDirector Instance { get; private set; }
    public List<CinmachineKeyBind> cameras;

    private CinemachineCamera currentCamera;

    public GameObject mainCharacter;
    public GameObject Target;

    private void Awake()
    {
       
        Instance = this;

    }
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

    public void SetTarget(GameObject target)
    {   
        mainCharacter= target;
        foreach (var bind in cameras)
        {
            if (bind.camera != null)
            {
                bind.camera.Target.TrackingTarget = target.transform;
            }
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
        SetTarget(mainCharacter);
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
