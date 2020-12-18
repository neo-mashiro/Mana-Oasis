using System.Collections.Generic;
using System.Linq;
using Players;
using UnityEngine;
using Utilities;

namespace Weapons {
    
    [RequireComponent(typeof(ObjectPool))]
    public class Wand : MonoBehaviour {
        
        private Queue<string> _spells;
        private string _currentSpell;
        private IFireable _currentMagic;
        
        private ObjectPool _magicPool;
        private AudioSource _audioSource;

        private void Start() {
            // need to use observer pattern here because Magics is changing over time
            // modify this code later
            var magics = GetComponent<PlayerStatus>().Magics;
            _spells = new Queue<string>(magics.Select(magic => magic.ToString()));
            
            SwitchToNextMagic();
            
            // object pool does not follow observer pattern, all available magics should be pre-instantiated when the
            // scene is loaded. Once the pool is created, it cannot change anymore.
            _magicPool = GetComponent<ObjectPool>().Pool;
            _audioSource = GetComponent<AudioSource>();
        }

        private void Update() {
            if (Input.GetKeyDown(KeyCode.Q)) {
                SwitchToNextMagic();
            }
            else if (Input.GetMouseButtonDown(0)) {
                _currentMagic = _magicPool.Fetch(_currentSpell).GetComponent<IFireable>();
                _currentMagic.Load();
            }
            else if (Input.GetMouseButtonUp(0)) {
                _currentMagic.Fire();
                _audioSource.Play();
            }
        }

        private void SwitchToNextMagic() {
            _currentSpell = _spells.Peek();
            _spells.Enqueue(_spells.Dequeue());
        }
    }
}