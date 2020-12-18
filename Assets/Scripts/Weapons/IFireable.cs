using System.Collections;
using UnityEngine;

namespace Weapons {
    
    public interface IFireable {

        /// <summary>
        /// Handles audio, animations and visual effects before the spell is cast or the ammo is fired.
        /// </summary>
        void Load();
        
        /// <summary>
        /// Fires the ammo or casts the spell.
        /// </summary>
        void Fire();

        /// <summary>
        /// Handles event on collision hit, to be called from OnCollisionEnter() or OnTriggerEnter().
        /// </summary>
        void OnHit(Collision other);

        /// <summary>
        /// The fireable object recycles itself by calling SetActive(false) in timeout seconds, so that it can be
        /// fetched and reused from the object pool. Depending on your use case, you may want to add extra functionality
        /// right before or after the object is recycled.<br/>
        /// <br/>
        /// In your MonoBehaviour class, while you can call this function from OnEnable(), it is not recommended.
        /// For high-speed fireables, it's better to place the call inside Fire(). For low-speed fireables whose
        /// flight time can vary (such as gravity holes), place the call in both Fire() and OnHit(), so that it will
        /// be recycled right after detecting a hit, or in timeout seconds if no hit is found. 
        /// </summary>
        IEnumerator Recycle(float timeout);

    }
}
