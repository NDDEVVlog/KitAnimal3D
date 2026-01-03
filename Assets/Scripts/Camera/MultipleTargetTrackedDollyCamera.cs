using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class MultipleTargetTrackedDollyCamera : MonoBehaviour
{
    [Header("Cinemachine")]
    [SerializeField] private CinemachineCamera cinemachine;

    [Header("Targets")]
    [SerializeField] private List<GameObject> targets = new();

    [Header("Selection")]
    [SerializeField] private float maxTrackDistance = 0f; // 0 = no limit
    [SerializeField] private float switchHysteresis = 0.25f; // reduce flicker when distances are similar
    [SerializeField] private float updateInterval = 0.05f; // seconds; 0 = every frame

    private Transform _currentTarget;
    private float _timer;

    private void Reset()
    {
        cinemachine = GetComponent<CinemachineCamera>();
    }

    private void Awake()
    {
        if (cinemachine == null) cinemachine = GetComponent<CinemachineCamera>();
    }

    private void Update()
    {
        if (cinemachine == null) return;

        if (updateInterval > 0f)
        {
            _timer += Time.deltaTime;
            if (_timer < updateInterval) return;
            _timer = 0f;
        }

        var closest = FindClosestTarget();
        if (closest == null)
        {
            // Optional: if no valid target, you can clear tracking or keep last.
            // cinemachine.Target.TrackingTarget = null;
            return;
        }

        if (_currentTarget == null)
        {
            SetTracking(closest);
            return;
        }

        // Hysteresis: only switch if new target is meaningfully closer.
        float currentDistSqr = (closest.position - transform.position).sqrMagnitude; // temporary reuse
        float oldDistSqr = (_currentTarget.position - transform.position).sqrMagnitude;

        // Convert hysteresis to squared comparison safely
        float h = Mathf.Max(0f, switchHysteresis);
        // Switch when: newDist < oldDist - hysteresis
        if (currentDistSqr < (oldDistSqr - h * h))
        {
            SetTracking(closest);
        }
    }

    private void SetTracking(Transform t)
    {
        _currentTarget = t;
        cinemachine.Target.TrackingTarget = t;
    }

    private Transform FindClosestTarget()
    {
        Transform best = null;
        float bestDistSqr = float.PositiveInfinity;

        Vector3 camPos = transform.position;

        for (int i = 0; i < targets.Count; i++)
        {
            var go = targets[i];
            if (go == null) continue;

            var tr = go.transform;
            float dSqr = (tr.position - camPos).sqrMagnitude;

            if (maxTrackDistance > 0f)
            {
                float maxSqr = maxTrackDistance * maxTrackDistance;
                if (dSqr > maxSqr) continue;
            }

            if (dSqr < bestDistSqr)
            {
                bestDistSqr = dSqr;
                best = tr;
            }
        }

        return best;
    }

    // Optional helpers if you add/remove targets at runtime
    public void AddTarget(GameObject target)
    {
        if (target != null && !targets.Contains(target))
            targets.Add(target);
    }

    public void RemoveTarget(GameObject target)
    {
        if (targets.Remove(target) && _currentTarget == target?.transform)
            _currentTarget = null;
    }
}
