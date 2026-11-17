/*
|-----------------------------------------------------------|
| Rotates the GameObject along a specified axis at a speed. |
|-----------------------------------------------------------|
*/

using UnityEngine;

[ExecuteAlways] // Allows running in Edit Mode when enabled
public class Rotation : MonoBehaviour
{
    // Enum for axis selection
    public enum RotationAxis { X, Y, Z }

    [Header("Rotation Settings")]
    [Tooltip("Enable to allow rotation in Edit Mode.")]
    public bool allowEditMode = false;

    [Tooltip("Select the axis to rotate around.")]
    public RotationAxis rotationAxis = RotationAxis.Y;

    [Tooltip("Rotation speed in degrees per second.")]
    public float rotationSpeed = 30f;

    [Tooltip("Use Local or World space for rotation.")]
    public Space rotationSpace = Space.Self;

    // Update is called once per frame
    void Update()
    {
        // Only rotate in Play Mode or when Edit Mode is enabled
        if (Application.isPlaying || allowEditMode)
        {
            Vector3 axisVector = GetAxisVector();
            transform.Rotate(axisVector * rotationSpeed * Time.deltaTime, rotationSpace);
        }
    }

    // Returns the selected axis as a vector
    private Vector3 GetAxisVector()
    {
        return rotationAxis switch
        {
            RotationAxis.X => Vector3.right,
            RotationAxis.Y => Vector3.up,
            RotationAxis.Z => Vector3.forward,
            _ => Vector3.zero
        };
    }
}

/*
|-----------------------------------------------------------|
| Made using Hexicosa Intelligence                          |
|-----------------------------------------------------------|
*/
