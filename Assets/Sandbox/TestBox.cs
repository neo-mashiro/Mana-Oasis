using System;
using System.Collections;
using System.Collections.Generic;
using Players;
using UnityEngine;
using Utilities;

namespace Sandbox {
    
    public class TestBox : MonoBehaviour {
        
        [SerializeField] private GameObject targetGameObject;

        private Component _component;

        private void Start() {
            _component = targetGameObject.GetComponent<PlayerStatus>();
        }
        
        private void Update() {
            if (Input.GetKeyDown(KeyCode.Alpha1)) {
                
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2)) {
                
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3)) {
                
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4)) {

            }
            else if (Input.GetKeyDown(KeyCode.Alpha5)) {

            }
        }
    }
}