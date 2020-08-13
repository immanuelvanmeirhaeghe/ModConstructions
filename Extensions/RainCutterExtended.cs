using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ModConstructions.Extensions
{
    class RainCutterExtended : RainCutter
    {
        public void SetBoxCollider(BoxCollider boxCollider)
        {
            try
            {
                m_Collider = boxCollider;
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{nameof(ModConstructions)}.{nameof(ModConstructions)}:{nameof(SetBoxCollider)}] throws exception: {exc.Message}");
            }
        }
    }
}
