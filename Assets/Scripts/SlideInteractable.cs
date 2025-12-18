using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class SlideInteractable : Interactable
{
    [Header("Slide Configuration")]
    [SerializeField] private List<Transform> _pathPoints; 
    [SerializeField] private float _slideDuration = 2.0f;

    public StageNode TargetNode; 

    public override async UniTask ExecuteInteraction(AnimalMotor motor)
    {   
        Debug.Log("Starting Slide Interaction");
        
        motor.transform.position = _pathPoints[0].position;
        motor.transform.rotation = _pathPoints[0].rotation;

        float segmentDuration = _slideDuration / (_pathPoints.Count - 1);
        var token = motor.GetCancellationTokenOnDestroy();

        for (int i = 0; i < _pathPoints.Count - 1; i++)
        {
            Vector3 start = _pathPoints[i].position;
            Vector3 end = _pathPoints[i+1].position;
            
            float elapsed = 0;
            while (elapsed < segmentDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / segmentDuration;
                
                motor.transform.position = Vector3.Lerp(start, end, t);
                
                Vector3 dir = (end - start).normalized;
                if(dir != Vector3.zero)
                    motor.transform.rotation = Quaternion.Slerp(motor.transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 10f);

                // Replaces yield return null
                await UniTask.NextFrame(token);
            }
        }
    }
}