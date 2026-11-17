/*
|-----------------------------------------------------------|
| Enemy                                                    |
|-----------------------------------------------------------|
| Handles enemy behaviors based on the selected movement    |
| type: Jump, Ghost Chase, or Roll Towards Player. Includes |
| a public Slay() method to destroy the enemy GameObject.   |
|-----------------------------------------------------------|
*/

using UnityEngine;
using UnityEngine.Events;

public class Enemy : MonoBehaviour
{
    // Enum for enemy movement type
    public enum MovementType
    {
        Jump,
        GhostChase,
        RollTowardsPlayer
    }

    [Header("General Settings")]
    [Tooltip("The movement type for the enemy.")]
    public MovementType movementType;

    [Tooltip("Speed of the enemy movement.")]
    public float speed = 5f;

    [Tooltip("Event triggered when the enemy catches the player.")]
    public UnityEvent onPlayerCaught;

    private Transform playerTransform;
    private Rigidbody rb;
    private Collider col;

    public GameObject spawnPrefabOnDeath;

    private float nextActionTime; // Time for the next movement action

    private void Start()
    {
        // Automatically tag this GameObject as "Enemy" if not already tagged
        if (gameObject.tag != "Enemy")
        {
            gameObject.tag = "Enemy";
        }

        // Locate the player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }

        SetupComponents();
        ScheduleNextAction();
    }

    private void Update()
    {
        if (playerTransform == null)
            return;

        switch (movementType)
        {
            case MovementType.Jump:
                HandleJump();
                break;

            case MovementType.GhostChase:
                HandleGhostChase();
                break;

            case MovementType.RollTowardsPlayer:
                HandleRollTowardsPlayer();
                break;
        }
    }

    private void HandleJump()
    {
        if (Time.time >= nextActionTime)
        {
            // Calculate jump direction
            Vector3 direction = playerTransform.position - transform.position;
            direction.y = 0; // Keep movement horizontal
            direction.Normalize();

            // Apply jump force
            rb.AddForce((direction + Vector3.up) * speed, ForceMode.Impulse);

            ScheduleNextAction();
        }
    }

    private void HandleGhostChase()
    {
        Vector3 direction = playerTransform.position - transform.position;
        direction.Normalize();

        // Move directly towards player
        transform.position += direction * speed * Time.deltaTime;
    }

    private void HandleRollTowardsPlayer()
    {
        if (Time.time >= nextActionTime)
        {
            // Calculate roll direction
            Vector3 direction = playerTransform.position - transform.position;
            direction.Normalize();

            // Apply roll force
            rb.AddForce(direction * speed, ForceMode.Impulse);

            ScheduleNextAction();
        }
    }

    private void SetupComponents()
    {
        // Ensure the necessary components are added or removed based on movement type
        switch (movementType)
        {
            case MovementType.Jump:
            case MovementType.RollTowardsPlayer:
                rb = GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = gameObject.AddComponent<Rigidbody>();
                }

                col = GetComponent<Collider>();
                if (col == null)
                {
                    col = gameObject.AddComponent<BoxCollider>();
                }
                col.isTrigger = false; // Use a solid collider
                break;

            case MovementType.GhostChase:
                rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Destroy(rb);
                }

                col = GetComponent<Collider>();
                if (col == null)
                {
                    col = gameObject.AddComponent<BoxCollider>();
                }
                col.isTrigger = true; // Use a trigger collider
                break;
        }
    }

    private void ScheduleNextAction()
    {
        // Calculate next action time based on speed
        float interval = 1 / speed;
        nextActionTime = Time.time + interval;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (movementType == MovementType.GhostChase && other.CompareTag("Player"))
        {
            onPlayerCaught?.Invoke();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (movementType != MovementType.GhostChase && collision.collider.CompareTag("Player"))
        {
            onPlayerCaught?.Invoke();
        }
    }

    /// <summary>
    /// Destroys the enemy GameObject.
    /// </summary>
    public void Slay()
    {
        if(spawnPrefabOnDeath) 
        {
            var prefabSpawned = Instantiate(spawnPrefabOnDeath, transform.position,  Quaternion.identity);
            Destroy(prefabSpawned, 4.0f);
        }

        Destroy(gameObject);
    }
}

/*
|-----------------------------------------------------------|
| Made using Hexicosa Intelligence                          |
|-----------------------------------------------------------|
*/
