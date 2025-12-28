using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AnimalSelection : MonoBehaviour
{
    [SerializeField] private List<GameObject> animals;     // prefabs
    [SerializeField] private AutoOrbitCamera cameraOrbit;

    private int currentIndex = 0;
    private GameObject currentInstance;

    private void Start()
    {
        SpawnCurrent();
    }

    public void NextAnimal(int direction)
    {
        currentIndex += direction;

        if (currentIndex < 0) currentIndex = animals.Count - 1;
        else if (currentIndex >= animals.Count) currentIndex = 0;

        SpawnCurrent();
    }

    private void SpawnCurrent()
    {
        // Destroy the scene instance, not the prefab asset
        if (currentInstance != null)
        {
            Destroy(currentInstance);
            currentInstance = null;
        }

        currentInstance = Instantiate(animals[currentIndex], transform);

        // Safely disable components if they exist
        if (currentInstance.TryGetComponent(out AnimalBrain brain)) brain.enabled = false;
        if (currentInstance.TryGetComponent(out AnimalMotor motor)) motor.enabled = false;
        if (currentInstance.TryGetComponent(out NavMeshAgent agent)) agent.enabled = false;

        if (cameraOrbit != null) cameraOrbit.SetTarget(currentInstance.transform);
    }

    public void SetSelectedAnimal()
    {
        SceneLoader.Instance.SetSpawnGameobject(animals[currentIndex]);
    }
}
