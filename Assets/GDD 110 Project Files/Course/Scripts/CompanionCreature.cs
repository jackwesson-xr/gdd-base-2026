using UnityEngine;

public class CompanionCreature : MonoBehaviour
{
    public float jumpForce = 5f; // Force of the jump
    public float minJumpInterval = 1f; // Minimum interval between jumps
    public float maxJumpInterval = 5f; // Maximum interval between jumps
    public float randomness = 0.3f; // Amount of randomness in the jump direction

    private Rigidbody rb;
    private float nextJumpTime;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        ScheduleNextJump();
    }

    private Camera GetCamera()
    {
        // Attempt to get the main camera
        return Camera.main;
    }

    void Update()
    {
        if (Time.time >= nextJumpTime)
        {
            Jump();
            ScheduleNextJump();
        }
    }

    private void Jump()
    {
        Camera cam = GetCamera(); // Get the active camera

        if (cam != null)
        {
            // Calculate the direction towards the camera
            Vector3 directionToCamera = cam.transform.position - transform.position;
            directionToCamera.y = 0; // Flatten to the horizontal plane
            directionToCamera.Normalize();

            // Add randomness to the direction
            Vector3 randomOffset = new Vector3(Random.Range(-randomness, randomness), 0, Random.Range(-randomness, randomness));
            Vector3 jumpDirection = (directionToCamera + randomOffset).normalized;

            // Apply force in the calculated direction
            rb.AddForce((jumpDirection + Vector3.up) * jumpForce, ForceMode.Impulse);
        }
        else
        {
            Debug.LogWarning("No main camera found.");
        }
    }

    private void ScheduleNextJump()
    {
        nextJumpTime = Time.time + Random.Range(minJumpInterval, maxJumpInterval);
    }
}
