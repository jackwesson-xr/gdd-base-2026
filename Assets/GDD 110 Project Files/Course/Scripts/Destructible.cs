/*
|-----------------------------------------------------------|
| DestroyOnCommand                                           |
|-----------------------------------------------------------|
| This script provides a public function that destroys      |
| the GameObject it is attached to. Includes a global debug |
| option for detailed logging.                              |
|-----------------------------------------------------------|
*/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class Destructible : MonoBehaviour
{
    // Header for configuration
    [Header("Configuration")]
    [Tooltip("Enable this to log debug messages.")]
    private bool debugEnabled = false;

    // Public method to destroy the GameObject
    /// <summary>
    /// Destroys the GameObject this script is attached to.
    /// </summary>
    public void TriggerDestruction()
    {
        if (debugEnabled)
        {
            Debug.Log($"[DestroyOnCommand] Destroying GameObject: {gameObject.name}", this);
        }

        Destroy(gameObject);
    }
}

/*
|-----------------------------------------------------------|
| Made using Hexicosa Intelligence                          |
|-----------------------------------------------------------|
*/

