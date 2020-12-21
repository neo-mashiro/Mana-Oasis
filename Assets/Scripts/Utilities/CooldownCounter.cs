using System;
using UnityEngine;

namespace Utilities {

    /// <summary>
    /// A convenient non-mono helper class to manage cooldown times. You can use the class to track elapsed time,
    /// spawn monsters in random x seconds, count skill cooldown, trigger events and animate cooldown clocks.<br/>
    /// <br/>
    /// For skill cooldown, create a non-loop instance of this class, each time the player casts the spell, reset the
    /// counter and start ticking in Update() until it's ready, you definitely don't want the counter to loop itself
    /// automatically. For enemy spawners, create a loop version so that you don't need to Reset() manually each time,
    /// in which case a random cooldown generator function can also be useful. You can always pause the counter by stop
    /// calling Tick() based on some condition checks, or simply call Tick(0) which is equally performant.<br/>
    /// <br/>
    /// This class is intended for game dependent events ONLY, which directly synchronize with the Time class in Unity.
    /// Due to floating point errors accumulated by using delta time, absolute time accuracy is not guaranteed. For
    /// precision critical and gameplay independent code, consider using the C# built-in Timer class or Stopwatch class
    /// instead. However, system time may not be in sync with Unity time, and the code won't change with time scale.
    /// </summary>
    public class CooldownCounter {
        
        // the cooldown float generator function
        private readonly Func<float> _cooldownFunction;
        
        public bool Loop { get; }
        
        public float Cooldown { get; private set; }
        
        public float TimeLeft { get; private set; }
        
        public float PercentLeft => Mathf.Clamp01(TimeLeft / Cooldown);
        
        public bool Ready => TimeLeft <= 0;
        
        /// <summary>
        /// Creates a new cooldown counter by specifying a constant cooldown in seconds.
        /// </summary>
        public CooldownCounter(bool loop, float cooldown) {
            if (cooldown > 0) {
                Loop = loop;
                Cooldown = TimeLeft = cooldown;
            }
            else {
                throw new ArgumentOutOfRangeException(nameof(cooldown));
            }
        }

        /// <summary>
        /// Creates a new cooldown counter where cooldown is defined by a function.
        /// </summary>
        public CooldownCounter(bool loop, Func<float> cooldownFunction) {
            var cooldown = cooldownFunction();
            if (cooldown > 0) {
                Loop = loop;
                Cooldown = TimeLeft = cooldown;
                _cooldownFunction = cooldownFunction;
            }
            else {
                throw new ArgumentOutOfRangeException(nameof(cooldown));
            }
        }

        /// <summary>
        /// Ticks the cooldown counter by x seconds or Time.deltaTime (default). Once cooldown is ready and loop is
        /// set to true, the counter will restart itself on the next frame update.<br/>
        /// <br/>
        /// You can pass in unscaled delta time to make the cooldown counter time-scale independent. Or if you need to
        /// temporarily accelerate ticks without resetting the cooldown, or speed up the remaining ticks while the
        /// counter is still running, use a multiple of delta time such as `1.5f * Time.deltaTime`. This makes it a
        /// breeze to simulate the effects of buffs/debuffs on skill cooldown.
        /// </summary>
        public void Tick(float? seconds = null) {
            // seconds ??= Time.deltaTime;  // C# 8.0 feature not yet supported in Unity
            seconds = seconds == null ? Time.deltaTime : seconds;

            if (seconds == 0) {
                return;
            }
            
            if (Ready) {
                if (!Loop) {
                    return;
                }
                Reset();
            }
            
            TimeLeft = Mathf.Max(TimeLeft - seconds.Value, 0);
        }

        /// <summary>
        /// Resets the cooldown counter.
        /// </summary>
        public void Reset() {
            var cooldown = _cooldownFunction?.Invoke() ?? Cooldown;
            Cooldown = TimeLeft = cooldown;
        }

        /// <summary>
        /// Resets the cooldown counter with a new cooldown.
        /// </summary>
        public void Reset(float cooldown) {
            Cooldown = TimeLeft = cooldown;
        }
    }
}