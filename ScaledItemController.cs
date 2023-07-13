using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EscapePodFour
{
    public class ScaledItemController : ScaledObjectController
    {
        public static List<ScaledItemController> All = new();

        public override float SizeMultiplier => 0.5f;

        void OnEnable()
        {
            All.Add(this);
        }

        void OnDisable()
        {
            All.Remove(this);
        }
    }
}
