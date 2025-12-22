using UnityEngine;
using Unity.Cinemachine;
public class FakeDirector : MonoBehaviour
{   
    public CinemachineCamera cinemachineCamera;
    public CinemachineCamera cinemachineCamera2;
    public Material material1;
    public Renderer targetRenderer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        material1 = targetRenderer.material;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {   
            var temp = cinemachineCamera.Priority;
            cinemachineCamera.Priority = cinemachineCamera2.Priority;
            cinemachineCamera2.Priority = temp;
        }
        if (Input.GetKeyDown(KeyCode.M))
        {
            material1.SetFloat("Process",Mathf.Lerp(1,0,Time.deltaTime));
        }
        
    }
}
