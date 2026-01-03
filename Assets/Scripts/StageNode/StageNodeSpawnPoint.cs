using JetBrains.Annotations;
using UnityEngine;

public class StageNodeSpawnPoint : StageNode
{
    public GameObject spawnedObject;
    public FakeDirector fakeDirector;
    public InteractionUI uiManager;
    public ControlMode controlMode;
    public void Start()
    {
        fakeDirector = FindFirstObjectByType<FakeDirector>();
    }
    public void SpawnAtPoint()
    {
        GameObject spawned = Instantiate(spawnedObject, transform.position, transform.rotation);
        fakeDirector.mainCharacter = spawned;
        spawned.GetComponent<AnimalBrain>().enabled = true;
        spawned.GetComponent<AnimalMotor>().enabled = true;
        spawned.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = true;
        spawned.GetComponent<AnimalBrain>()._startingNode = this;
        spawned.GetComponent<AnimalBrain>()._uiManager = uiManager;
        spawned.GetComponent<AnimalBrain>()._currentControlMode = controlMode;
        fakeDirector.SetTarget(spawned);
        
    }
}
