using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Utilities;

namespace Managers {
    
    /// <summary>
    /// [!Caution] This is the sealed singleton class for game management.
    /// </summary>
    public sealed class GameManager : MonoSingleton<GameManager> {
        
        // define your data
        
        // define your method
        // on scene unloaded, fade into the switch UI (UI between scenes, with an eased lerping loading bar)
        // on scene loaded, fade away the switch UI

        protected override void OnAwake() {
            // your custom initialization code
        }
    }
}
