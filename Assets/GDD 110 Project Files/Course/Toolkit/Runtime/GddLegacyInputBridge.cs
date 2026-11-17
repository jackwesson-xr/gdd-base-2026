using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
namespace GDD110
{
    // Adds controller support without rewriting any serialized legacy script or event.
    [DefaultExecutionOrder(-50),DisallowMultipleComponent]
    public class GddLegacyInputBridge : GddEntity
    {
        ButtonClickTrigger[] triggers;
        GddInteractor interactor;
        readonly HashSet<ButtonClickTrigger> suppressed=new HashSet<ButtonClickTrigger>();
        void Start(){triggers=FindObjectsByType<ButtonClickTrigger>(FindObjectsInactive.Include);interactor=GetComponent<GddInteractor>();}
        void Update()
        {
            if(triggers==null)return;
            foreach(var trigger in triggers)
            {
                if(trigger==null)continue;
                bool useToolkit=interactor!=null&&((trigger.targetKey==KeyCode.Mouse0&&interactor.Equipped!=null)||(trigger.targetKey==KeyCode.E&&interactor.Candidate!=null));
                if(useToolkit){if(trigger.enabled){trigger.enabled=false;suppressed.Add(trigger);}continue;}
                if(suppressed.Remove(trigger))trigger.enabled=true;
                if(!trigger.isActiveAndEnabled)continue;
                var pad=Gamepad.current;if(pad==null)continue;ButtonControl button=null;
                switch(trigger.targetKey)
                {
                    case KeyCode.E:button=pad.buttonWest;break;
                    case KeyCode.Mouse0:button=pad.rightTrigger;break;
                    case KeyCode.Space:button=pad.buttonSouth;break;
                    case KeyCode.Tab:button=pad.buttonNorth;break;
                    case KeyCode.UpArrow:button=pad.dpad.up;break;
                    case KeyCode.DownArrow:button=pad.dpad.down;break;
                }
                if(button!=null&&button.wasPressedThisFrame)trigger.onKeyPressed.Invoke();
                if(button!=null&&button.wasReleasedThisFrame)trigger.onKeyReleased.Invoke();
            }
        }
        void OnDisable(){foreach(var trigger in suppressed)if(trigger!=null)trigger.enabled=true;suppressed.Clear();}
    }
}
