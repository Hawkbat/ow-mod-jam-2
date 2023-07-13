using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace EscapePodFour
{
    public abstract class ScaledObjectController : MonoBehaviour
    {
        public float Scale = 1f;

        public float Size { get => Scale * SizeMultiplier; set => Scale = value / SizeMultiplier; }

        float currentScale;
        float previousScale;

        public abstract float SizeMultiplier { get; }

        protected virtual void Awake()
        {
            Scale = transform.localScale.z;
            currentScale = Scale;
        }

        protected virtual void FixedUpdate()
        {
            currentScale = Mathf.MoveTowards(currentScale, Scale, Time.deltaTime);
            UpdateScale(currentScale, previousScale);
            previousScale = currentScale;
        }

        protected virtual void UpdateScale(float newScale, float oldScale)
        {
            transform.localScale = Vector3.one * newScale;
        }
    }
}
