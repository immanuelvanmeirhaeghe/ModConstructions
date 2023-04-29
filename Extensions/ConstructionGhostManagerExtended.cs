using System.Linq;
using UnityEngine;

namespace ModConstructions.Extensions
{
    class ConstructionGhostManagerExtended : ConstructionGhostManager
    {
        protected override void Update()
        {
                if ( (ModConstructions.Get().IsModActiveForSingleplayer || ModConstructions.Get().IsModActiveForMultiplayer)
                    && ModConstructions.Get().InstantFinishConstructionsOption  && Input.GetKeyDown(KeyCode.F8))
                {
                    foreach (ConstructionGhost m_Unfinished in m_AllGhosts.Where(
                                              m_Ghost => m_Ghost.gameObject.activeSelf
                                                                       && m_Ghost.GetState() != ConstructionGhost.GhostState.Ready))
                    {
                        m_Unfinished.SetState(ConstructionGhost.GhostState.Ready);
                    }
                }
                base.Update();
        }
    }
}
