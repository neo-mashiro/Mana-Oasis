using TMPro;
using UnityEngine;

namespace Utilities {
    
    /// <summary>
    /// A time-scale independent device that counts down from a specified time interval.<br/>
    /// <br/>
    /// For devices that tick at a fixed interval such as timers and stopwatches, it's easier to work with delta time,
    /// but this approach suffers from floating point imprecision. If the timer's countdown is a very large float
    /// (99:59:59 requires 359,999 seconds), and you add or subtract very small numbers (unscaled delta time) from it
    /// over and over again, you introduce rounding errors that can accumulate. A robust solution is to compare real
    /// time to a fixed start timestamp.
    /// </summary>
    public class Timer : MonoBehaviour {

        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField, Range(0, 359999)] private int countdown = 3600;
        
        private bool _stopped = true;

        private float _elapsedTime;
        private float _startTime;
        private float _pauseTime;

        /// <summary>
        /// Checks if the timer's time is up. Use this property to start a WaitUntil() coroutine, UI event, etc.
        /// </summary>
        public bool TimeIsUp { get; private set; }
        
        /// <summary>
        /// Number of seconds to count down. Setting this property will update and reset the timer ready for a fresh start.
        /// </summary>
        public int Countdown {
            get => countdown;
            set {
                countdown = Mathf.Clamp(value, 0, 359999);
                ResetTick();
            }
        }

        private void Start() => ResetTick();

        private void Update() {
            if (!_stopped) {
                if (Countdown > _elapsedTime) {
                    _elapsedTime = Time.realtimeSinceStartup - _startTime;
                    SetTime(Countdown - _elapsedTime);
                }
                else {
                    _stopped = true;
                    TimeIsUp = true;
                    SetTime(-1);
                }
            }
        }

        public void StartTick() {
            if (_stopped) {
                // fresh start
                if (_startTime < 0) {
                    _startTime = Time.realtimeSinceStartup - 0.1f;
                    _stopped = false;
                }
                // resume count
                else if (_pauseTime > 0) {
                    _startTime += Time.realtimeSinceStartup - _pauseTime;
                    _pauseTime = -1;
                    _stopped = false;
                }
            }
        }

        public void PauseTick() {
            if (!_stopped) {
                _stopped = true;
                _pauseTime = Time.realtimeSinceStartup;
            }
        }

        public void ResetTick() {
            _stopped = true;
            _startTime = _pauseTime = -1;
            _elapsedTime = 0;
            TimeIsUp = false;
            SetTime(Countdown - 0.1f);
        }
        
        private void SetTime(float time) {
            time += 1;  // keep the whole second until its fractions are used up
            
            var hours = Mathf.FloorToInt(time / 3600);
            var minutes = Mathf.FloorToInt(time % 3600 / 60);
            var seconds = Mathf.FloorToInt(time % 60);

            timeText.SetText("{0:00}:{1:00}:{2:00}", hours, minutes, seconds);
        }
    }
}
