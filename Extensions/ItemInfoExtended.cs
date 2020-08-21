namespace ModConstructions
{
    /// <summary>
    /// Extends ItemInfo
    /// specifically to override the method
    /// to determine whether an item is a shelter or not.
    /// </summary>
    class ItemInfoExtended : ItemInfo
    {
        /// <summary>
        /// Modded to enable all shelter types.
        /// </summary>
        /// <returns>Always true</returns>
        public new bool IsShelter()
        {
            if (ModConstructions.Get().IsModActiveForSingleplayer || ModConstructions.Get().IsModActiveForMultiplayer)
            {
                return m_ID.ToString().ToLower().EndsWith("shelter");
            }
            return base.IsShelter();
        }
    }
}
