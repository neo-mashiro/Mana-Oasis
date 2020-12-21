using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Utilities;

namespace Manager {
    
    /// <summary>
    /// [!Caution] This is the sealed singleton class for music management.
    /// </summary>
    public sealed class MusicManager : MonoSingleton<GameManager> {
        
        // define your data (data for streaming your BGM resources)
        
        // define your method
        // on scene unloaded, fade the volume away
        // on scene loaded, fade in the new BGM

        protected override void OnAwake() {
            // your custom initialization code to play the opening music
        }
    }
}
