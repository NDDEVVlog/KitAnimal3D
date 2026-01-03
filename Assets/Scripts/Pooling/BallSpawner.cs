using UnityEngine;

public class BallSpawner : MonoBehaviour
{
    [Header("Pool Settings")]
    [SerializeField] private Ball _ballPrefab;
    [SerializeField] private int _maxActiveBalls = 15;

    [Header("Spawn Settings")]
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private float _force = 20f;

    private FixedPool<Ball> _pool;

    private void Awake()
    {
        _pool = new FixedPool<Ball>(_ballPrefab, _maxActiveBalls, transform);
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Spawn();
        }
    }

    public void SpawnNumberOfBall(int number)
    {
        for(int i = 0 ; i< number; i++)
        {
            Spawn();
        }
    }

    private void Spawn()
    {
        Ball ball = _pool.Get();
        
        ball.Initialize(_pool);
        ball.transform.SetPositionAndRotation(_spawnPoint.position, Random.rotation); // Corrected method name
        
        ball.Launch(_spawnPoint.forward * _force);
    }
}