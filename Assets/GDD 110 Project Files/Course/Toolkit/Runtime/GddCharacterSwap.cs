using System.Collections.Generic;
using UnityEngine;

namespace GDD110
{
    /// <summary>
    /// Remembers a player character swap (Tools > GDD 110 > Character > Swap Player Character) so it can be undone
    /// with Revert. Added and removed by the swap tool; nothing to set up by hand.
    /// </summary>
    [DisallowMultipleComponent, AddComponentMenu("")]
    public class GddCharacterSwap : MonoBehaviour
    {
        [System.Serializable]
        public class Moved
        {
            public Transform item;
            public Transform originalParent;
            public Vector3 localPosition;
            public Quaternion localRotation;
            public Vector3 localScale;
            public bool createdCopy;   // a fresh socket made because the original couldn't be moved
        }

        [Tooltip("The character model that was swapped in.")]
        public GameObject model;
        public string modelAssetPath;
        public Avatar originalAvatar;
        public Avatar swappedAvatar;
        public List<GameObject> hiddenOriginals = new List<GameObject>();
        public List<Moved> moved = new List<Moved>();
    }
}
