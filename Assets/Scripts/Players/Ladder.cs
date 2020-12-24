using System.Diagnostics;
using UnityEngine;

namespace Players {
    
    [RequireComponent(typeof(BoxCollider))]
    public class Ladder : MonoBehaviour {
        
        [Tooltip("A vector relative to the ladder's transform position that defines the bottom anchor point.")]
        [SerializeField] private Vector3 vectorToBottomAnchor = new Vector3(0, 0.3f, 0.5f);
        [SerializeField] private float ladderSegmentLength;

        private Vector3 BottomAnchorPoint => transform.position + transform.TransformVector(vectorToBottomAnchor);
        private Vector3 TopAnchorPoint => BottomAnchorPoint + transform.up * ladderSegmentLength;
        
        [SerializeField] private Transform bottomReleasePoint;  // where we leave the ladder from top
        [SerializeField] private Transform topReleasePoint;  // where we leave the ladder from bottom

        public Transform BottomReleasePoint => bottomReleasePoint;
        public Transform TopReleasePoint => topReleasePoint;

        /// <summary>
        /// Returns the closet point on the ladder plane from our character, and outputs a float deviation representing
        /// how far that point is from the ladder. A negative deviation means the closest point is below the ladder's
        /// bottom anchor point, a positive deviation means above the ladder's top anchor point. If the closet point
        /// lies In between the top and bottom anchor points, deviation is always zero.
        /// </summary>
        public Vector3 ClosestPointOnLadder(Vector3 fromPoint, out float deviation) {
            var ladderSegment = TopAnchorPoint - BottomAnchorPoint;            
            var path = fromPoint - BottomAnchorPoint;
            var offsetOnLadderPlane = Vector3.Dot(path, ladderSegment.normalized);

            // higher than bottom anchor point
            if (offsetOnLadderPlane > 0) {
                // in between the top and bottom anchor point
                if (offsetOnLadderPlane <= ladderSegment.magnitude) {
                    deviation = 0;
                    return BottomAnchorPoint + ladderSegment.normalized * offsetOnLadderPlane;
                }
                // higher than top anchor point
                deviation = offsetOnLadderPlane - ladderSegment.magnitude;
                return TopAnchorPoint;
            }
            // lower than bottom anchor point
            deviation = offsetOnLadderPlane;
            return BottomAnchorPoint;
        }

        [Conditional("UNITY_EDITOR")]
        private void OnDrawGizmos() {
            var midpoint = (BottomAnchorPoint + TopAnchorPoint) * 0.5f;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(BottomAnchorPoint, midpoint);
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(midpoint, TopAnchorPoint);
        }
    }
}