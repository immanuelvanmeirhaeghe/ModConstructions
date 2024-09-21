using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModConstructions.Extensions
{
    public class ConstructionGhostExtended : ConstructionGhost
    {
        public override void UpdateProhibitionType(bool check_is_snapped = true, Construction decoration_align_construction = null)
        {
            m_ProhibitionType = ProhibitionType.None;
            return;
        }
    }
}
