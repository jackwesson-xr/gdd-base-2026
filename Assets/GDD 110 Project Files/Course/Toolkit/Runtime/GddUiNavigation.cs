using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace GDD110
{
    [DisallowMultipleComponent,RequireComponent(typeof(EventSystem))]
    public class GddUiNavigation:GddEntity
    {
        public Selectable firstSelection;
        void Start(){SelectFirst();}
        void Update()
        {
            if(EventSystem.current==null||EventSystem.current.currentSelectedGameObject!=null)return;
            var pad=Gamepad.current;
            if(pad!=null&&(pad.dpad.ReadValue().sqrMagnitude>0.1f||pad.leftStick.ReadValue().sqrMagnitude>0.1f||pad.buttonSouth.wasPressedThisFrame))SelectFirst();
        }
        public void SelectFirst()
        {
            if(firstSelection==null||!firstSelection.IsActive()||!firstSelection.IsInteractable())return;
            if(EventSystem.current!=null){EventSystem.current.firstSelectedGameObject=firstSelection.gameObject;EventSystem.current.SetSelectedGameObject(firstSelection.gameObject);}
        }
    }
}
