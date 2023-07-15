using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace EscapePodFour
{
    public static class Extensions
    {
        public static void SetWorldScale(this Transform transform, Vector3 scale)
        {
            transform.localScale = Vector3.one;
            transform.localScale = new Vector3(scale.x / transform.lossyScale.x, scale.y / transform.lossyScale.y, scale.z / transform.lossyScale.z);
        }
    }
}
