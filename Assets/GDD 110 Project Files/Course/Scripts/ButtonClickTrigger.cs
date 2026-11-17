/*
|-----------------------------------------------------------|
| KeyCode Input Trigger                                     |
|-----------------------------------------------------------|
| This script listens for input based on any KeyCode and    |
| triggers a UnityEvent when the respective key is pressed. |
|-----------------------------------------------------------|
*/

using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
[ExecuteAlways] // Allows the script to run in edit mode if enabled
public class ButtonClickTrigger : MonoBehaviour
{
    [Header("Input Settings")]
    [Tooltip("Select the KeyCode to listen for.")]
    public KeyCode targetKey = KeyCode.Space;

    [Header("Events")]
    [Tooltip("UnityEvent triggered when the target key is pressed.")]
    public UnityEvent onKeyPressed;
    public UnityEvent onKeyReleased;

    [Header("Debug Settings")]
    [Tooltip("Enable to log key press events.")]
    private bool debugMode = false;

    [Tooltip("Allow script execution in edit mode.")]
    private bool allowEditModeUpdates = false;

    private void Update()
    {
        // Ensure script runs in play mode or edit mode (if enabled)
        if (!Application.isPlaying && !allowEditModeUpdates) return;

        // Check if the target key is pressed
        if (Input.GetKeyDown(targetKey))
        {
            if (debugMode)
                Debug.Log($"[KeyCodeInputTrigger] Key '{targetKey}' was pressed.");

            // Trigger the UnityEvent
            onKeyPressed?.Invoke();
        }
         if (Input.GetKeyUp(targetKey))
        {
            if (debugMode)
                Debug.Log($"[KeyCodeInputTrigger] Key '{targetKey}' was pressed.");

            // Trigger the UnityEvent
            onKeyReleased?.Invoke();
        }
    }
}

/*
|-----------------------------------------------------------|
| Made using Hexicosa Intelligence                          |
|-----------------------------------------------------------|
*/
