using System;
using UnityEngine;

namespace Players {
    
    public enum Perspective {
        FirstPerson,
        ThirdPerson
    }

    public enum ControlMode {
        Default,
        Auto,  // reserve for future use
        Free,
        Swim,
        Climb
    }

    public enum ClimbState {
        Anchor,
        Climb,
        DeAnchor
    }

    public enum OrientationMode {
        TowardsGravity,
        TowardsGravityAndSlope
    }
    
    public struct PlayerCameraInputs {
        public float MovementX;
        public float MovementY;
        public float ZoomInput;
        public bool SwitchView;
    }

    public struct PlayerCharacterInputs {
        public float MovementZ;
        public float MovementX;
        public bool JumpDown;
        public bool JumpHeld;
        public bool CrouchDown;
        public bool CrouchUp;
        public bool CrouchHeld;
        public bool FreeModeToggled;
        public bool ClimbModeToggled;
    }

    public struct AICharacterInputs {
        public Vector3 MoveVector;
        public Vector3 LookVector;
    }

    // use ushort (0 to 65,535) as the enum underlying type, we can limit the number of buffs up to 15 (2^16 = 65,536)
    // this is a reasonable limit as more buffs would introduce overcomplexity and mess up gameplay
    [Flags]
    public enum PlayerBuff : ushort {
        None = 0,
        Berserk = 1 << 0,   // increase ATK, MAG, LUK
        Bless   = 1 << 1,   // reduce mana cost
        Dream   = 1 << 2,   // reduce skill cooldown
        Shield  = 1 << 3,   // increase DEF
        Reflect = 1 << 4,   // reflect 30% of the damage
        Block   = 1 << 5    // immune to all debuffs
    }
    
    // use ushort (0 to 65,535) as the enum underlying type, we can limit the number of debuffs up to 15 (2^16 = 65,536)
    // this is a reasonable limit as more debuffs would introduce overcomplexity and mess up gameplay
    [Flags]
    public enum PlayerDebuff : ushort {
        None = 0,
        Poison  = 1 << 0,   // reduce health every few seconds
        Freeze  = 1 << 1,   // unable to act for a few seconds
        Silence = 1 << 2,   // unable to use skills
    }

    public enum Magic {
        Heal,     // recover health
        Fireball,
        ElectricShock,
        GravityHole
    }

}
