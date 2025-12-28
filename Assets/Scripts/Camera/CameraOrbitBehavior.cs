using UnityEngine;

public class AutoOrbitCamera : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Transform _target;
    [SerializeField] private float _rotationSpeed = 20f;
    [SerializeField] private float _lookAtHeightOffset = 1.0f;

    public  float _distance;
    private float _height;
    private float _currentAngle;

    private void Start()
    {
        if (_target == null) return;

        // Calculate initial relative position from Scene View placement
        Vector3 diff = transform.position - _target.position;
        
        // Horizontal distance (radius)
        _distance = new Vector2(diff.x, diff.z).magnitude;
        
        // Vertical height
        _height = diff.y;
        
        // Initial angle so camera doesn't snap to a new position on Play
        _currentAngle = Mathf.Atan2(diff.x, diff.z) * Mathf.Rad2Deg;
    }

    private void LateUpdate()
    {
        if (_target == null) return;

        // Increment angle
        _currentAngle += _rotationSpeed * Time.deltaTime;
        
        // Calculate new position using Trigonometry
        float rad = _currentAngle * Mathf.Deg2Rad;
        float x = Mathf.Sin(rad) * _distance;
        float z = Mathf.Cos(rad) * _distance;

        Vector3 targetPos = _target.position;
        
        // Update Position
        transform.position = new Vector3(
            targetPos.x + x, 
            targetPos.y + _height, 
            targetPos.z + z
        );

        // Look at the target (plus offset to look at head/body instead of feet)
        transform.LookAt(targetPos + Vector3.up * _lookAtHeightOffset);
    }
    public void SetTarget(Transform newTarget)
    {
        //_target = newTarget;
    }
}