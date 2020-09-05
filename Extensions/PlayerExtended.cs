using UnityEngine;

namespace ModConstructions
{
    class PlayerExtended : Player
    {
        protected override void Start()
        {
            base.Start();
            new GameObject($"__{nameof(ModConstructions)}__").AddComponent<ModConstructions>();
        }
    }
}
