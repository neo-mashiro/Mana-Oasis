using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Utilities {
    
    public static class Calendar {

        public enum ManaTime {
            Midnight,   // 23:00 ~ 01:00
            Night,      // 01:00 ~ 05:00
            Morning,    // 05:00 ~ 11:00
            Midday,     // 11:00 ~ 13:00
            Afternoon,  // 13:00 ~ 17:00
            Evening     // 17:00 ~ 23:00
        }
        
        public enum ManaSeason { Spring, Summer, Autumn, Winter }

        private static readonly TimeSpan SunriseEpoch = new TimeSpan(7, 0, 0);
        private static readonly TimeSpan SunsetEpoch = new TimeSpan(18, 30, 0);
        private static readonly TimeSpan LunarSpan = new TimeSpan(12, 0, 0);

        public static readonly string[] Anniversaries = {"Genesis", "Goddess", "Christmas", "Carnival"};
        
        public static readonly DateTime Genesis = new DateTime(DateTime.UtcNow.Year, 2, 16);
        public static readonly DateTime Goddess = new DateTime(DateTime.UtcNow.Year, 7, 16);
        public static readonly DateTime Christmas = new DateTime(DateTime.UtcNow.Year, 12, 25);
        public static readonly DateTime Carnival = new DateTime(DateTime.UtcNow.Year, 12, 31);

        /// <summary>
        /// Returns the current datetime in the world of Mana-Oasis (300 years later). The mana datetime does not
        /// take into account local time zones and DST, but instead uses the universal UTC time globally.
        /// This property is not intended to be queried every frame. If you want to display time in a clock, cache
        /// this in a variable, increment it by the elapsed real time (Time.realtimeSinceStartup), and then resync
        /// with UTC time every few hours.
        /// </summary>
        public static DateTime GetManaDate => DateTime.UtcNow.AddYears(300);

        /// <summary>
        /// Returns the current mana season, which affects terrain weathers in Restopia.
        /// </summary>
        public static ManaSeason GetManaSeason() {
            var thisMonth = GetManaDate.Month;

            if (thisMonth >= 12 || thisMonth <= 2) {
                return ManaSeason.Winter;
            }
            
            if (thisMonth <= 5) {
                return ManaSeason.Spring;
            }
            
            return thisMonth <= 8 ? ManaSeason.Summer : ManaSeason.Autumn;
        }

        /// <summary>
        /// Returns the current mana time, which affects mana concentration in Restopia.
        /// </summary>
        public static ManaTime GetManaTime() {
            var thisHour = GetManaDate.TimeOfDay.Hours;

            if (thisHour < 1 || thisHour >= 23) {
                return ManaTime.Midnight;
            }

            if (thisHour < 5) {
                return ManaTime.Night;
            }
            
            if (thisHour < 11) {
                return ManaTime.Morning;
            }
            
            if (thisHour < 13) {
                return ManaTime.Midday;
            }
            
            return thisHour < 17 ? ManaTime.Afternoon : ManaTime.Evening;
        }
        
        /// <summary>
        /// Returns the current mana sunrise time, which can be used to control skybox and clock events.
        /// </summary>
        public static TimeSpan GetSunriseTime() {
            var deltaHours = Mathf.Cos(Mathf.Clamp(GetManaDate.DayOfYear, 0, 360) * Mathf.Deg2Rad);
            return SunriseEpoch + TimeSpan.FromHours(deltaHours);
        }
        
        /// <summary>
        /// Returns the current mana sunset time, which can be used to control skybox and clock events.
        /// </summary>
        public static TimeSpan GetSunsetTime() {
            var deltaHours = Mathf.Cos(Mathf.Clamp(GetManaDate.DayOfYear, 0, 360) * Mathf.Deg2Rad);
            return SunsetEpoch - TimeSpan.FromHours(1.5f * deltaHours);
        }
        
        /// <summary>
        /// Returns the current mana moonrise time, which can be used to control skybox and clock events.
        /// </summary>
        public static TimeSpan GetMoonriseTime() {
            return GetSunsetTime() + TimeSpan.FromHours(Random.Range(0.1f, 2f));
        }
        
        /// <summary>
        /// Returns the current mana moonset time, which can be used to control skybox and clock events.
        /// </summary>
        public static TimeSpan GetMoonsetTime() {
            return (GetMoonriseTime() + LunarSpan).Subtract(TimeSpan.FromHours(24));
        }
    }
}
