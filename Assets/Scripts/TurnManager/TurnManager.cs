using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    public enum PickMode { ManualMenu, AutoRandom ,Sequence}

    [Serializable] public class ParticipantEvent : UnityEvent<TurnParticipant, int> { }

    [Header("Participants")]
    [SerializeField] private List<TurnParticipant> participants = new();

    [Header("Mode")]
    [SerializeField] private PickMode pickMode = PickMode.ManualMenu;
    [SerializeField] private bool avoidSameAsCurrent = true;

    [Header("Input")]
    [SerializeField] private KeyCode nextTurnKey = KeyCode.Space;

    [Header("Runtime")]
    [SerializeField] private int currentIndex = -1;
    private bool waitingForRandomInput;
    
    [Header("Events")]
    public ParticipantEvent OnTurnStarted;
    public ParticipantEvent OnTurnEnded;
    public UnityEvent OnRequestPickNext; // Manual menu

    UniTaskCompletionSource<bool> pickDone;
    private System.Random _rng;

    public IReadOnlyList<TurnParticipant> Participants => participants;
    public int CurrentIndex => currentIndex;
    public TurnParticipant Current =>
        (currentIndex >= 0 && currentIndex < participants.Count) ? participants[currentIndex] : null;

    int idx = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        _rng = new System.Random();
    }

    public async void Start()
    {
        await StartFirstTurn();
    }

    private void Update()
    {
        if (waitingForRandomInput && Input.GetKeyDown(nextTurnKey))
        {
            waitingForRandomInput = false;
            ExecuteRandomNextTurn();
        }
    }

    /* ================= PUBLIC API ================= */

    public void SetParticipants(List<TurnParticipant> list)
    {
        participants = list ?? new List<TurnParticipant>();
        currentIndex = -1;
    }

    public void SetMode(PickMode mode) => pickMode = mode;

    public async Task StartFirstTurn()
    {
        if (pickMode == PickMode.ManualMenu)
        {   
            pickDone = new UniTaskCompletionSource<bool>();
            OnRequestPickNext?.Invoke();

            await pickDone.Task.AsTask();

            Debug.Log("PickDone");
            return;
        }

        ExecuteRandomNextTurn(ignoreCurrent: false);
    }

    public void SelectTurn(int index)
    {
        if (index < 0 || index >= participants.Count) return;
        var p = participants[index];
        if (p == null || !p.IsValidForTurn) return;

        currentIndex = index;

        p.BeginTurn();
        OnTurnStarted?.Invoke(p, currentIndex);

        if (FakeDirector.Instance != null)
        {
            FakeDirector.Instance.SetTarget(participants[index].gameObject);
            Debug.Log("Set Target Success to :" + participants[index].gameObject.name);
        }
            
    }

    /// <summary>
    /// Gọi khi bạn muốn kết thúc lượt hiện tại
    /// </summary>
    public void EndTurn()
    {   Debug.Log("EndTurn");
        if (Current != null)
        {
            Current.EndTurn();
            OnTurnEnded?.Invoke(Current, currentIndex);
        }

        if (pickMode == PickMode.ManualMenu)
        {   

            OnRequestPickNext?.Invoke();
            return;
        }
        waitingForRandomInput = true;
    }

    /* ================= INTERNAL ================= */
    public void NotifyPickCompleted()
    {
        pickDone?.TrySetResult(true);
    }

    private void ExecuteRandomNextTurn(bool ignoreCurrent = true)
    {   
        // Thêm kiểm tra an toàn
        if (participants == null || participants.Count == 0)
        {
            Debug.LogWarning("Danh sách participants đang rỗng!");
            return;
        }

        switch (pickMode)
        {
            case PickMode.AutoRandom: 
                idx = PickRandomIndex(ignoreCurrent && avoidSameAsCurrent);
                break;
            case PickMode.Sequence:
                idx++;
                if(idx >= participants.Count) idx = 0; // Sửa lỗi logic: idx > count thành idx >= count
                break;
        }
         
        // Chỉ select nếu index hợp lệ
        if (idx >= 0 && idx < participants.Count)
        {   
            Debug.Log("Select: " + idx);
            SelectTurn(idx);
        }
        else
        {
            currentIndex = -1;
        }
    }

    private int PickRandomIndex(bool ignoreCurrent)
    {
        var valid = new List<int>();

        for (int i = 0; i < participants.Count; i++)
        {
            var p = participants[i];
            if (p == null || !p.IsValidForTurn) continue;
            if (ignoreCurrent && i == currentIndex) continue;
            valid.Add(i);
        }

        if (valid.Count == 0)
        {
            if (currentIndex >= 0 && currentIndex < participants.Count &&
                Current != null && Current.IsValidForTurn)
                return currentIndex;

            return -1;
        }

        return valid[_rng.Next(0, valid.Count)];
    }
}
