using UnityEngine;

namespace x.Restopia.Scripts.Museum {
    
    public class SurfaceMorpher : MonoBehaviour {
	    [SerializeField] private ComputeShader computeShader = default;
	    [SerializeField] private Mesh mesh = default;
	    [SerializeField] private Material material = default;
	    
	    [SerializeField, Range(24, 96)] private int resolution = 96;
		
	    [SerializeField, Tooltip("Name of the first Math surface to render.")]
	    private MathSurface.Surface surface = default;

		[SerializeField, Min(0f), Tooltip("Time in seconds before transitioning to the next surface.")]
		private float duration = 2f;
		
		private static readonly int
			StepId = Shader.PropertyToID("_Step"),
			TimeId = Shader.PropertyToID("_Time"),
			ScaleId = Shader.PropertyToID("_Scale"),
			PositionsId = Shader.PropertyToID("_Positions"),
			ResolutionId = Shader.PropertyToID("_Resolution"),
			TransitionProgressId = Shader.PropertyToID("_TransitionProgress");

		private float _timeCount;
		private bool _inTransition;
		private MathSurface.Surface _currentSurface;

		private ComputeBuffer _positionsBuffer;

		private void Awake() {
			// enforce resolution to be a multiple of 8 so that GPU can compute in parallel
			resolution = Mathf.FloorToInt(resolution / 8f) * 8;
		}

		private void OnEnable () {
			// each position vector3 has 3 float numbers, so 3 times 4 bytes
			_positionsBuffer = new ComputeBuffer(resolution * resolution, 3 * 4);
		}

		private void OnDisable () {
			_positionsBuffer.Release();
			_positionsBuffer = null;
		}

		private void Update () {
			_timeCount += Time.deltaTime;
			if (_inTransition) {
				if (_timeCount >= 1) {  // 1 sec of transition time
					_timeCount--;
					_inTransition = false;
				}
			}
			else if (_timeCount >= duration) {
				_timeCount -= duration;
				_inTransition = true;
				_currentSurface = surface;
				surface = MathSurface.GetNextOnGPU(surface);
			}
		
			UpdateSurfaceOnGPU();
		}

		private void UpdateSurfaceOnGPU () {
			// set the step size, resolution, and time properties of the compute shader
			var step = 2f / resolution;
			computeShader.SetFloat(StepId, step);
			computeShader.SetInt(ResolutionId, resolution);
			computeShader.SetFloat(TimeId, Time.time);
			
			if (_inTransition) {
				computeShader.SetFloat(TransitionProgressId, Mathf.SmoothStep(0f, 1f, _timeCount));
			}

			int kernelIndex;
			if (_inTransition) {
				var kernelName = _currentSurface.ToString("G") + "To" + surface.ToString("G");
				kernelIndex = computeShader.FindKernel(kernelName);
			}
			else {
				kernelIndex = computeShader.FindKernel(surface.ToString("G"));
			}
			
			// link the compute buffer to GPU
			computeShader.SetBuffer(kernelIndex, PositionsId, _positionsBuffer);

			// dispatch the kernel to update positions in the buffer
			var groups = Mathf.CeilToInt(resolution / 8f);
			computeShader.Dispatch(kernelIndex, groups, groups, 1);
		
			// now that the compute buffer contains updated positions, send them to the material shader
			material.SetBuffer(PositionsId, _positionsBuffer);
			material.SetVector(ScaleId, new Vector4(step, 1f / step));
			
			// instruct GPU to draw mesh within a bounding box (cube) of size 2 (plus step size on border)
			var bounds = new Bounds(Vector3.zero, Vector3.one * (2f + step));
			Graphics.DrawMeshInstancedProcedural(mesh, 0, material, bounds, _positionsBuffer.count);
		}
    }
}