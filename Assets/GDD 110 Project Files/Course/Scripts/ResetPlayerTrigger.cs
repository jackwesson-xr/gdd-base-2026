/*
|-----------------------------------------------------------|
| ResetSceneOnTrigger                                       |
|-----------------------------------------------------------|
| This script resets the current scene when the player      |
| enters a trigger zone. Automatically ensures the trigger  |
| collider is configured correctly.                         |
|-----------------------------------------------------------|
*/

using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class ResetPlayerTrigger : MonoBehaviour
{
    // Player tag constant
    private const string playerTag = "Player";

    public void Reset()
    {
        // Automatically ensure a Collider component is attached and set as trigger
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider == null)
        {
            triggerCollider = gameObject.AddComponent<BoxCollider>();
            Debug.Log("[ResetSceneOnTrigger] No collider found. BoxCollider added.", this);
        }

        triggerCollider.isTrigger = true;
    }

    private void Awake()
    {
        // Ensure the collider is set as a trigger
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null && !triggerCollider.isTrigger)
        {
            triggerCollider.isTrigger = true;
            Debug.Log("[ResetSceneOnTrigger] Collider set to trigger mode.", this);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the entering object matches the player tag
        if (other.CompareTag(playerTag))
        {
            Debug.Log("[ResetSceneOnTrigger] Player entered trigger zone. Resetting scene...", this);

            // Reset the current scene
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
    private void OnCollisionEnter(Collision other) {
        // Check if the entering object matches the player tag
        if (other.gameObject.CompareTag(playerTag))
        {
            Debug.Log("[ResetSceneOnTrigger] Player entered trigger zone. Resetting scene...", this);

            // Reset the current scene
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}

/*
|-----------------------------------------------------------|
| Made using Hexicosa Intelligence                          |
|-----------------------------------------------------------|
*/
