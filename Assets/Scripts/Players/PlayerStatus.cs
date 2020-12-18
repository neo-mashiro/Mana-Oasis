using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using JetBrains.Annotations;
using UnityEngine;
using Utilities;
using Random = UnityEngine.Random;

namespace Players {

    public class PlayerStatus : MonoBehaviour {
        
        [Header("Health and Mana")]
        [SerializeField] [Range(100, 9999)] private int maxHealthPoint = 100;
        [SerializeField] [Range(100, 9999)] private int maxManaPoint = 100;

        public int HealthPoint { get; private set; }
        public int ManaPoint { get; private set; }

        // stats
        public string PlayerName { get; private set; }
        public int Level { get; private set; } = 1;
        public int ManaCoin { get; private set; } = 10000;
        public bool IsDead => HealthPoint <= 0;

        public PlayerBuff Buffs { get; private set; } = PlayerBuff.None;
        public PlayerDebuff Debuffs { get; private set; } = PlayerDebuff.None;

        // attributes
        public int ATK { get; private set; } = 10;
        public int DEF { get; private set; } = 10;
        public int MAG { get; private set; } = 10;
        public int LUK { get; private set; } = 10;  // increase chances of double damage
        
        // magic skills
        public Queue<Magic> Magics { get; } = new Queue<Magic>();
        

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// New Player Status
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public void NewPlayer(string playerName) {
            PlayerName = playerName;
            ResetStatus();
        }
        
        public void ResetStatus() {
            Level = 1;
            maxHealthPoint = maxManaPoint = 100;
            HealthPoint = ManaPoint = 100;
            ATK = DEF = MAG = LUK = 10;
            ClearBuffs();
            ClearDebuffs();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// Health and Mana
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        public void TakeDamage(int damage) {
            HealthPoint -= Mathf.Max(damage - DEF, 0);
            HealthPoint = Mathf.Clamp(HealthPoint, 0, maxHealthPoint);
        }

        public bool ConsumeMana(int mana) {
            if (ManaPoint < mana) {
                return false;
            }
            
            ManaPoint -= mana;
            ManaPoint = Mathf.Clamp(ManaPoint, 0, maxManaPoint);
            return true;
        }
        
        public void RecoverHealth(int amount) {
            if (HealthPoint < maxHealthPoint) {
                HealthPoint += amount;
                HealthPoint = Mathf.Clamp(HealthPoint, 0, maxHealthPoint);
            }
        }
        
        public void RecoverMana(int amount) {
            if (ManaPoint < maxManaPoint) {
                ManaPoint += amount;
                ManaPoint = Mathf.Clamp(ManaPoint, 0, maxManaPoint);
            }
        }
        
        public void RecoverHealthAndMana(int amount) {
            RecoverHealth(amount);
            RecoverMana(amount);
        }
        
        [ContextMenu("Recover HP/MP")]
        public void RecoverAllHealthAndMana() {
            HealthPoint = maxHealthPoint;
            ManaPoint = maxManaPoint;
        }
        
        public void RecoverHealthPercent(float percent) {
            if (HealthPoint < maxHealthPoint) {
                HealthPoint += Convert.ToInt32(HealthPoint * Mathf.Clamp01(percent));
                HealthPoint = Mathf.Clamp(HealthPoint, 0, maxHealthPoint);
            }
        }
        
        public void RecoverManaPercent(float percent) {
            if (ManaPoint < maxManaPoint) {
                ManaPoint += Convert.ToInt32(ManaPoint * Mathf.Clamp01(percent));
                ManaPoint = Mathf.Clamp(ManaPoint, 0, maxManaPoint);
            }
        }
        
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// Buffs and Debuffs
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        /// <summary>
        /// If you need to set multiple buffs at one go, use bitwise or operator like this:<br/>
        /// SetBuff(Playerbuff.Bless | Playerbuff.Reflect, true);
        /// </summary>
        public void SetBuff(PlayerBuff buff, bool active) {
            if (active) {
                Buffs |= buff;
            }
            else {
                Buffs &= ~buff;
            }
        }
        
        /// <summary>
        /// If you need to set multiple debuffs at one go, use bitwise or operator like this:<br/>
        /// SetDebuff(PlayerDebuff.Poison | PlayerDebuff.Freeze, false);
        /// </summary>
        public void SetDebuff(PlayerDebuff debuff, bool active) {
            if (active) {
                Debuffs |= debuff;
            }
            else {
                Debuffs &= ~debuff;
            }
        }

        [ContextMenu("Clear Buffs")]
        public void ClearBuffs() {
            Buffs = PlayerBuff.None;
        }
        
        [ContextMenu("Clear Debuffs")]
        public void ClearDebuffs() {
            Debuffs = PlayerDebuff.None;
        }
        
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// Level
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        [ContextMenu("Level Up")]
        public void LevelUp() {
            Level += 1;
            Level = Mathf.Clamp(Level, 1, 100);
            maxHealthPoint += Random.Range(90, 105);
            maxManaPoint += Random.Range(90, 105);
            ATK += Random.Range(8, 10);
            DEF += Random.Range(8, 10);
            MAG += Random.Range(8, 10);
            LUK += Random.Range(8, 10);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// Economy
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        [ContextMenu("Get ManaCoins")]
        public void GetCoins(int amount) {
            ManaCoin += amount;
        }

        // public void BuyItem(Item item) {
        //     ManaCoin -= item.price;
        // }

        // public void UseItem(Item item) {
        //     switch (item) {
        //         case Crystal:
        //             MAG += Random.Range(1, 3);
        //             break;
        //     }
        // }
        
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// Magic and Skills
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public void LearnMagic(Magic magic) {
            Magics.Enqueue(magic);
        }
    }
}
