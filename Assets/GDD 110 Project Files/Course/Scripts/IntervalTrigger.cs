/*
|-----------------------------------------------------------|
| IntervalTrigger                                           |
|-----------------------------------------------------------|
| This script triggers a UnityEvent at regular intervals    |
| specified by a public time value in seconds.              |
|-----------------------------------------------------------|
*/

using UnityEngine;
using UnityEngine.Events;

public class IntervalTrigger : MonoBehaviour
{
    [Header("Interval Settings")]
    [Tooltip("Time in seconds between each trigger.")]
    [Range(0.1f, 60f)]
    public float intervalInSeconds = 1f;

    [Header("Events")]
    [Tooltip("UnityEvent triggered at each interval.")]
    public UnityEvent atEachInterval;

    private float elapsedTime = 0f;

    private void Update()
    {
        // Accumulate time
        elapsedTime += Time.deltaTime;

        // Check if it's time to trigger the event
        if (elapsedTime >= intervalInSeconds)
        {
            elapsedTime -= intervalInSeconds; // Reset elapsed time
            atEachInterval?.Invoke();        // Trigger the UnityEvent
        }
    }
}

/*
|-----------------------------------------------------------|
| Made using Hexicosa Intelligence                          |
|-----------------------------------------------------------|
*/
