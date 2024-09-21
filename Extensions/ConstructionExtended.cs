using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModConstructions.Extensions
{
    public class ConstructionExtended : Construction
    {
		public override void SetUpperLevel(bool set, int level)
		{
			if (ModConstructions.IsModEnabled)
			{
                m_UpperLevel = set;
                m_Level = 0;              
            }
            else
            {
                m_UpperLevel = set;
                m_Level = level;
            }
            base.OnSetUpperLevel(set);
        }
	}
}
