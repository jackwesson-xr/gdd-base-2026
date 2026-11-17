using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TriggerActivatedInteraction : MonoBehaviour
{
    public UnityEvent whenPlayerEntersTrigger;
    public UnityEvent whenPlayersLeavesTrigger;

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag == "Player")
        {
            whenPlayerEntersTrigger.Invoke();
        }
    }
     private void OnTriggerExit(Collider other)
    {
        if(other.gameObject.tag == "Player")
        {
            whenPlayersLeavesTrigger.Invoke();
        }
    }

}
