using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

[RequireComponent(typeof(Rigidbody))]
public class Ball : MonoBehaviour, IPoolable
{
    [SerializeField] private float _lifeTime = 4f;
    [SerializeField] private Rigidbody _rb;

    private FixedPool<Ball> _originPool;
    private CancellationTokenSource _cts;

    public void Initialize(FixedPool<Ball> pool)
    {
        _originPool = pool;
    }

    public void Launch(Vector3 velocity)
    {
        _rb.linearVelocity = velocity;
        _rb.angularVelocity = Vector3.zero;
    }

    public void OnSpawn()
    {
        _rb.isKinematic = false;
        
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        
        LifeCycleAsync(_cts.Token).Forget();
    }

    public void OnDespawn()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _rb.isKinematic = true;
    }

    private async UniTaskVoid LifeCycleAsync(CancellationToken token)
    {
        bool canceled = await UniTask.Delay(System.TimeSpan.FromSeconds(_lifeTime), cancellationToken: token)
                                     .SuppressCancellationThrow();

        if (!canceled)
        {
            _originPool.Return(this);
        }
    }
}