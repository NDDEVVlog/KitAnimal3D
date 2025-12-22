using Cysharp.Threading.Tasks;
using UnityEngine;

public class VehicleInteractable : Interactable
{
    [SerializeField] private Transform _seatPoint;
    [SerializeField] private Transform _dismountPoint;
    [SerializeField] private bool _isMounting; 

    public override async UniTask ExecuteInteraction(AnimalMotor motor)
    {
        if (_isMounting)
        {
            motor.transform.SetParent(_seatPoint);
            motor.transform.localPosition = Vector3.zero;
            motor.transform.localRotation = Quaternion.identity;
        }
        else
        {
            motor.transform.SetParent(null);
            motor.transform.position = _dismountPoint.position;
            motor.transform.rotation = _dismountPoint.rotation;
        }

        // Wait for animation transition
        // Pass the motor's cancellation token so this stops if the object is destroyed
        await UniTask.Delay(500, cancellationToken: motor.GetCancellationTokenOnDestroy());
    }
}