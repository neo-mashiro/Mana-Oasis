using UnityEngine;

namespace Utilities {
	
	public class Destructor : MonoBehaviour {
		[SerializeField, Range(0f, 600f)] private float timeout = 5.0f;
		[SerializeField] private bool detachChildren = default;

		private void Awake() {
			Invoke(nameof(Suicide), timeout);
		}

		private void Suicide() {
			if (detachChildren) { transform.DetachChildren(); }
			Destroy(gameObject);
		}
	}
}