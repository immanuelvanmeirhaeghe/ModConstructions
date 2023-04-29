using ModConstructions.Managers;
using UnityEngine;

namespace ModConstructions.Extensions
{
    class PlayerExtended : Player
    {
        protected override void Start()
        {
            base.Start();
            new GameObject($"__{nameof(ModConstructions)}__").AddComponent<ModConstructions>();
            new GameObject($"__{nameof(StylingManager)}__").AddComponent<StylingManager>();
        }
    }
}
