using UnityEngine;
namespace GDD110
{
    [AddComponentMenu("GDD 110/Functions/Spin or Bob")]
    public class GddMotion : GddEntity
    {
        public Vector3 rotationPerSecond=new Vector3(0,45,0);
        [Min(0)] public float bobHeight;
        [Min(0)] public float bobSpeed=1;
        Vector3 origin;
        void OnEnable(){origin=transform.localPosition;}
        void Update(){transform.Rotate(rotationPerSecond*Time.deltaTime,Space.Self);if(bobHeight>0)transform.localPosition=origin+Vector3.up*(Mathf.Sin(Time.time*bobSpeed)*bobHeight);}
    }
}
