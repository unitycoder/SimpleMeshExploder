using System.Collections;
using System.Collections.Generic;
using Unitycoder.Demos;
using UnityEngine;

namespace Unitycoder.Demos
{
    public class BreakOnCollision : MonoBehaviour
    {
        [Tooltip("Collision impact threshold")]
        public float breakForce = 10f;

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.relativeVelocity.magnitude > breakForce)
            {
                // NOTE this can cause pieces to fly too much..
                SimpleMeshExploder.instance.Explode(transform);
            }
        }
    }
}
