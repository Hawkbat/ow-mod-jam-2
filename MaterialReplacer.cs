using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace EscapePodFour
{
    public class MaterialReplacer : MonoBehaviour
    {
        public Material MaterialToReplace;
        public string PathToVanillaRenderer;

        void Start()
        {
            var vanillaMat = GameObject.Find(PathToVanillaRenderer).GetComponent<Renderer>().sharedMaterial;
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                if (r.sharedMaterial == MaterialToReplace)
                {
                    r.sharedMaterial = vanillaMat;
                }
            }
        }
    }
}
