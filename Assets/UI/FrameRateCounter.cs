using System;
using UnityEngine;
using TMPro;
using Utilities;

namespace UI {
    
    /// <summary>
    /// Computes approximate framerate by measuring the duration between Unity frames, note that
    /// this is only estimate since Unity's update loop is not synchronized with the display.
    /// </summary>
    public class FrameRateCounter : MonoBehaviour {
        
        [SerializeField, Range(0.1f, 2f)] private float sampleInterval = 1f;
        
        private TextMeshProUGUI _avg, _range;
        private int _frames;
        private float _duration, _bestDuration, _worstDuration;

        private void Start() {
            ResetCounter();
            var children = transform.GetAllDirectChildren();
            
            foreach (var child in children) {
                switch (child.name) {
                    case "Average":
                        _avg = child.GetComponent<TextMeshProUGUI>();
                        break;
                    case "Range":
                        _range = child.GetComponent<TextMeshProUGUI>();
                        break;
                }
            }
        }

        private void Update() {
            var frameDuration = Time.unscaledDeltaTime;
            _frames += 1;
            _duration += frameDuration;

            if (frameDuration < _bestDuration) {
                _bestDuration = frameDuration;
            }
            if (frameDuration > _worstDuration) {
                _worstDuration = frameDuration;
            }

            if (_duration >= sampleInterval) {
                _avg.SetText("FPS {0:0} ({1:0}ms)", _frames / _duration, 1000f * _duration / _frames);
                _range.SetText("[min, max] {0:0}~{1:0}ms", 1000f * _bestDuration, 1000f * _worstDuration);
                ResetCounter();
            }
        }

        private void ResetCounter() {
            _frames = 0;
            _duration = 0f;
            _bestDuration = Single.MaxValue;
            _worstDuration = 0f;
        }
    }
}