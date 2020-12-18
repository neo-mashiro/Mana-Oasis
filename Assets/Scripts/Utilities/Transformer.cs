using System;
using UnityEngine;

namespace Utilities {
    
    public class Transformer : MonoBehaviour {
        private enum Motions {Translate, Rotate};
        private enum Axes {X, Y, Z}

        [SerializeField] private Motions motion = Motions.Translate;
        [SerializeField] private Axes axis = Axes.X;
        [SerializeField] private float speed = 1.0f, magnitude = 1f;

        private Vector3 _direction;

        private void Update() {
            switch (axis) {
                case Axes.X:
                    _direction = Vector3.right;
                    break;
                case Axes.Y:
                    _direction = Vector3.up;
                    break;
                case Axes.Z:
                    _direction = Vector3.forward;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        
            if (motion == Motions.Translate) {
                transform.Translate(_direction *
                                    (Mathf.Cos(Time.time * speed) * magnitude * speed * 0.01f));
            }
            else {
                transform.Rotate(_direction * (Time.deltaTime * speed * Mathf.Rad2Deg));
            }
        }
    }
}
