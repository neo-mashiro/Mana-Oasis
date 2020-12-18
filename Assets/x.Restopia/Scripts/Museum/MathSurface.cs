using UnityEngine;
using Random = UnityEngine.Random;

namespace x.Restopia.Scripts.Museum {
	
	public static class MathSurface {
	    // a static math surface is a one-to-many mapping from a 2D mesh grid to 3D space:
	    // one (u, v) -> many (x, y, z)
	    // on top of that, a dynamic surface also takes into account the time dimension t:
	    // one (u, v, t) -> many (x, y, z), where t changes over time
	    // 
	    // this general form can describe any curve, map, static and dynamic surface in math so
	    // we can model anything from 1D curves, 2D planes to surfaces in differential geometry
        public delegate Vector3 Equation (float u, float v, float t);

		public enum Surface { Wave, Ripple, Sphere, Torus }

		// choose the next surface to morph from the GPU compute shader (4 kernels)
		public static Surface GetNextOnGPU(Surface surface) {
			var nextSurface = (Surface) Random.Range(1, 4);
			return surface == nextSurface ? 0 : nextSurface;
		}
	}
}

