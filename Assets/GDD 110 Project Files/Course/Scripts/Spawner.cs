using UnityEngine;
using System.Collections.Generic;

public class Spawner : MonoBehaviour
{
    /*
    |-----------------------------|
    | Public Variables            |
    |-----------------------------|
    */

    [Tooltip("The prefab to spawn.")]
    public GameObject prefabToSpawn;

    [Tooltip("The location where the prefab should be spawned.")]
    public Transform spawnLocation;

    [Tooltip("The location where the display copy of the prefab will appear.")]
    public Transform displayPoint;

    [Tooltip("The interval in seconds between spawns.")]
    public float spawnInterval = 2f;

    [Tooltip("The maximum number of active spawned prefabs allowed.")]
    public int globalSpawnLimit = 5;

    [Tooltip("Enable or disable debug logs.")]
    public bool enableDebug = false;

    /*
    |-----------------------------|
    | Private Variables           |
    |-----------------------------|
    */

    private List<GameObject> activeInstances = new List<GameObject>();
    private GameObject displayPrefabInstance;
    private float spawnTimer;

    /*
    |-----------------------------|
    | Unity Methods               |
    |-----------------------------|
    */

    private void Start()
    {
        if (!prefabToSpawn || !spawnLocation || !displayPoint)
        {
            Debug.LogError("Spawner is missing required references!");
            return;
        }

        // Create the display prefab
        CreateDisplayPrefab();
    }

    private void Update()
    {
        activeInstances.RemoveAll(item => item == null);

        if (activeInstances.Count < Mathf.Max(1, globalSpawnLimit))
        {
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= Mathf.Max(0.05f, spawnInterval))
            {
                SpawnPrefab();
                spawnTimer = 0f;
            }
        }

        // Remove null (destroyed) objects from the active instances list
        activeInstances.RemoveAll(item => item == null);
    }

    /*
    |-----------------------------|
    | Private Methods             |
    |-----------------------------|
    */

    private void CreateDisplayPrefab()
    {
        // Instantiate the display prefab and make it a child of the display point
        displayPrefabInstance = Instantiate(prefabToSpawn, displayPoint.position, displayPoint.rotation, displayPoint);

        // Match scale and rotation to the display point
        displayPrefabInstance.transform.localScale = displayPoint.localScale;
        displayPrefabInstance.transform.localRotation = displayPoint.localRotation;

        // Disable any Enemy.cs scripts on the display prefab
        Enemy[] enemyScripts = displayPrefabInstance.GetComponentsInChildren<Enemy>();
        foreach (var script in enemyScripts)
        {
            script.enabled = false;
        }

        // Optionally, disable unnecessary components for clarity
        displayPrefabInstance.name = $"{prefabToSpawn.name}_Display";
    }

    private void SpawnPrefab()
    {
        // Instantiate the prefab at the spawn location
        GameObject newPrefabInstance = Instantiate(prefabToSpawn, spawnLocation.position, spawnLocation.rotation);
        activeInstances.Add(newPrefabInstance);

        if (enableDebug)
        {
            Debug.Log($"Spawned a new instance of {prefabToSpawn.name}. Active instances: {activeInstances.Count}");
        }
    }

    /*
    |-----------------------------|
    | Debugging Helper Methods    |
    |-----------------------------|
    */

    private void OnDrawGizmosSelected()
    {
        if (spawnLocation)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(spawnLocation.position, 0.5f);
        }

        if (displayPoint)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(displayPoint.position, 0.5f);
        }
    }
}

/* 
|-----------------------------------------------------------|
| Made using Hexicosa Intelligence                          |
|-----------------------------------------------------------|
*/
