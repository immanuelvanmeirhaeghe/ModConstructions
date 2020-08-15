using UnityEngine;

namespace ModConstructions.Extensions
{
    /// <summary>
    /// Inject modding interface into game only in single player mode
    /// </summary>
    class PlayerExtended : Player
    {
        protected override void Start()
        {
            base.Start();
            new GameObject("__ModConstructions__").AddComponent<ModConstructions>();
        }
    }
}
