using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EmissionToggle : MonoBehaviour
{
    private Renderer objectRenderer;
    private Material objectMaterial;
    private bool isEmissive = false;

    // UnityEvents for toggling emission on and off
    public UnityEvent OnEmissionEnabled;
    public UnityEvent OnEmissionDisabled;

    // Start is called before the first frame update
    void Start()
    {
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
        {
            objectMaterial = objectRenderer.material;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E)) // Press 'E' to toggle emission
        {
            ToggleEmission();
        }
    }

    void ToggleEmission()
    {
        if (objectMaterial != null)
        {
            isEmissive = !isEmissive;
            if (isEmissive)
            {
                objectMaterial.EnableKeyword("_EMISSION");
                objectMaterial.SetColor("_EmissionColor", Color.white); // Set the emission color to white or any other color
                OnEmissionEnabled?.Invoke(); // Trigger the event for enabling emission
            }
            else
            {
                objectMaterial.DisableKeyword("_EMISSION");
                objectMaterial.SetColor("_EmissionColor", Color.black); // Set the emission color to black to disable it
                OnEmissionDisabled?.Invoke(); // Trigger the event for disabling emission
            }
        }
    }
}
