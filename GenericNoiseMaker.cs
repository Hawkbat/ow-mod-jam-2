using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EscapePodFour
{
    public class GenericNoiseMaker : NoiseMaker
    {
        public float NoiseRadius { get => _noiseRadius; set => _noiseRadius = value; }

        void Update()
        {
            _noiseRadius = NoiseRadius;
        }
    }
}
