using Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ModConstructions
{
    class ModConstructions : MonoBehaviour
    {
        private static ModConstructions s_Instance;

        private bool showUI = false;

        private static ItemsManager itemsManager;

        private static Player player;

        private static HUDManager hUDManager;

        public bool IsModConstructionsActive = false;

        public static bool HasUnlockedConstructions = false;

        public static bool HasUnlockedRestingPlaces = false;

        public static bool TestRainFXInfoShown = false;

        //public bool IsTestRainFxEnabled;

        public bool IsOptionInstantFinishConstructionsActive;

        public bool IsLocalOrHost => ReplTools.AmIMaster();

        /// <summary>
        /// ModAPI required security check to enable this mod feature for multiplayer.
        /// See <see cref="ModManager"/> for implementation.
        /// Based on request in chat: use  !requestMods in chat as client to request the host to activate mods for them.
        /// </summary>
        /// <returns>true if enabled, else false</returns>
        public bool IsModActiveForMultiplayer => FindObjectOfType(typeof(ModManager.ModManager)) != null ? ModManager.ModManager.AllowModsForMultiplayer : false;

        public ModConstructions()
        {
            IsModConstructionsActive = true;
            s_Instance = this;
        }

        public static ModConstructions Get()
        {
            return s_Instance;
        }

        private void Update()
        {
            if ( (IsLocalOrHost || IsModActiveForMultiplayer) && Input.GetKeyDown(KeyCode.End))
            {
                if (!showUI)
                {
                    hUDManager = HUDManager.Get();

                    itemsManager = ItemsManager.Get();

                    player = Player.Get();

                    EnableCursor(true);
                }
                // toggle menu
                showUI = !showUI;
                if (!showUI)
                {
                    EnableCursor(false);
                }

                //if (IsTestRainFxEnabled)
                //{
                //    UpdateRainTest();
                //}
            }
        }

        private static void UpdateRainTest()
        {
            if (RainManager.Get().IsRain())
            {
                ShowHUDBigInfo("Testing rain FX - check beneath roofs!", "ModConstructions Info", HUDInfoLogTextureType.Count.ToString());
                TestRainFXInfoShown = true;
                RainProofing();
            }
            else
            {
                TestRainFXInfoShown = false;
            }
        }

        private void OnGUI()
        {
            if (showUI)
            {
                InitData();
                InitModUI();
            }
        }

        private static void InitData()
        {
            hUDManager = HUDManager.Get();

            itemsManager = ItemsManager.Get();

            player = Player.Get();

            InitSkinUI();
        }

        private static void InitSkinUI()
        {
            GUI.skin = ModAPI.Interface.Skin;
        }

        private void InitModUI()
        {
            GUI.Box(new Rect(500f, 500f, 450f, 150f), "ModConstructions UI - Press END to open/close", GUI.skin.window);

            GUI.Label(new Rect(520f, 520f, 200f, 20f), "Click to unlock all constructions", GUI.skin.label);
            if (GUI.Button(new Rect(770f, 520f, 150f, 20f), "Unlock constructions", GUI.skin.button))
            {
                OnClickUnlockConstructionsButton();
                showUI = false;
                EnableCursor(false);
            }

            GUI.Label(new Rect(520f, 540f, 200f, 20f), "Click to unlock all shelters and beds", GUI.skin.label);
            if (GUI.Button(new Rect(770f, 540f, 150f, 20f), "Unlock resting places", GUI.skin.button))
            {
                OnClickUnlockRestingPlacesButton();
                showUI = false;
                EnableCursor(false);
            }

            GUI.Label(new Rect(520f, 560f, 200f, 20f), "Use F8 to instantly finish constructions", GUI.skin.label);
            IsOptionInstantFinishConstructionsActive = GUI.Toggle(new Rect(770f, 560f, 20f, 20f), IsOptionInstantFinishConstructionsActive, "");

            GUI.Label(new Rect(520f, 580f, 200f, 20f), "Click to get a military bed", GUI.skin.label);
            if (GUI.Button(new Rect(770f, 580f, 150f, 20f), "Get military bed", GUI.skin.button))
            {
                OnClickGetMilitaryBedButton();
                showUI = false;
                EnableCursor(false);
            }
            //GUI.Label(new Rect(520f, 600f, 200f, 20f), "Test rain FX", GUI.skin.label);
            //IsTestRainFxEnabled = GUI.Toggle(new Rect(770f, 600f, 20f, 20f), IsTestRainFxEnabled, "");
        }

        public static void OnClickGetMilitaryBedButton()
        {
            try
            {
                Item bed = SpawnMilitaryBedToUse();
                ShowHUDBigInfo($"Created 1 x {bed.m_Info.GetNameToDisplayLocalized()}", "Mod Constructions Info", HUDInfoLogTextureType.Count.ToString());
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{nameof(ModConstructions)}.{nameof(ModConstructions)}:{nameof(OnClickUnlockConstructionsButton)}] throws exception: {exc.Message}");
            }
        }

        public bool UseOptionF8
        {
            get
            {
                return IsOptionInstantFinishConstructionsActive;
            }
        }

        public static void OnClickUnlockConstructionsButton()
        {
            try
            {
                UnlockAllConstructions();
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{nameof(ModConstructions)}.{nameof(ModConstructions)}:{nameof(OnClickUnlockConstructionsButton)}] throws exception: {exc.Message}");
            }
        }

        public static void OnClickUnlockRestingPlacesButton()
        {
            try
            {
                UnlockAllRestingPlaces();
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{nameof(ModConstructions)}.{nameof(ModConstructions)}:{nameof(OnClickUnlockRestingPlacesButton)}] throws exception: {exc.Message}");
            }
        }

        public static void UnlockAllConstructions()
        {
            try
            {
                if (!HasUnlockedConstructions)
                {
                    List<ItemInfo> list = itemsManager.GetAllInfos().Values.Where(info => info.IsConstruction()).ToList();
                    foreach (ItemInfo constructionItemInfo in list)
                    {
                        itemsManager.UnlockItemInNotepad(constructionItemInfo.m_ID);
                        itemsManager.UnlockItemInfo(constructionItemInfo.m_ID.ToString());
                        ShowHUDInfoLog(constructionItemInfo.m_ID.ToString(), "HUD_InfoLog_NewEntry");
                    }
                    HasUnlockedConstructions = true;
                }
                else
                {
                    ShowHUDBigInfo("All constructions were already unlocked", "Mod Constructions Info", HUDInfoLogTextureType.Count.ToString());
                }
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{nameof(ModConstructions)}.{nameof(ModConstructions)}:{nameof(UnlockAllConstructions)}] throws exception: {exc.Message}");
            }
        }

        public static void RainProofing()
        {
            try
            {
                List<Construction> roofs = Construction.s_AllRoofs;
                foreach (Construction roof in roofs)
                {
                    Vector3Int roofPosition = Vector3Int.FloorToInt(roof.transform.position);
                    ModAPI.Log.Write($"Roof location x: {roofPosition.x} y: {roofPosition.y} z: {roofPosition.z}");
                    //RainCutter roofCutter = new RainCutter
                    //{
                    //    m_Type = RainCutterType.Tent
                    //};
                    //((RainCutterExtended)roofCutter).SetBoxCollider(roof.m_BoxCollider);
                    //RainManager.Get().RegisterRainCutter(((RainCutterExtended)roofCutter));
                }
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{nameof(ModConstructions)}.{nameof(ModConstructions)}:{nameof(RainProofing)}] throws exception: {exc.Message}");
            }
        }

        public static void UnlockAllRestingPlaces()
        {
            try
            {
                if (!HasUnlockedRestingPlaces)
                {
                    List<ItemInfo> list = itemsManager.GetAllInfos().Values.Where(info => info.IsShelter()).ToList();

                    UnlockShelters(list);

                    UnlockBeds(list);

                    foreach (ItemInfo restingPlaceItemInfo in list)
                    {
                        itemsManager.UnlockItemInNotepad(restingPlaceItemInfo.m_ID);
                        itemsManager.UnlockItemInfo(restingPlaceItemInfo.m_ID.ToString());
                        ShowHUDInfoLog(restingPlaceItemInfo.m_ID.ToString(), "HUD_InfoLog_NewEntry");
                    }
                    HasUnlockedRestingPlaces = true;
                }
                else
                {
                    ShowHUDBigInfo("All resting places were already unlocked!", "Mod Constructions Info", HUDInfoLogTextureType.Count.ToString());
                }
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{nameof(ModConstructions)}.{nameof(ModConstructions)}:{nameof(UnlockAllRestingPlaces)}] throws exception: {exc.Message}");
            }
        }

        public static void UnlockBeds(List<ItemInfo> list)
        {
            if (!list.Contains(itemsManager.GetInfo(ItemID.Leaves_Bed)))
            {
                list.Add(itemsManager.GetInfo(ItemID.Leaves_Bed));
            }
            if (!list.Contains(itemsManager.GetInfo(ItemID.Leaves_Bed)))
            {
                list.Add(itemsManager.GetInfo(ItemID.Leaves_Bed));
            }
            if (!list.Contains(itemsManager.GetInfo(ItemID.banana_leaf_bed)))
            {
                list.Add(itemsManager.GetInfo(ItemID.banana_leaf_bed));
            }
            if (!list.Contains(itemsManager.GetInfo(ItemID.Logs_Bed)))
            {
                list.Add(itemsManager.GetInfo(ItemID.Logs_Bed));
            }
            if (!list.Contains(itemsManager.GetInfo(ItemID.BambooLog_Bed)))
            {
                list.Add(itemsManager.GetInfo(ItemID.BambooLog_Bed));
            }
        }

        public static Item SpawnMilitaryBedToUse()
        {
            itemsManager.UnlockItemInfo(ItemID.military_bed_toUse.ToString());
            Item militaryBed = itemsManager.CreateItem(ItemID.military_bed_toUse.ToString(), true);
            return militaryBed;
        }

        public static void UnlockShelters(List<ItemInfo> list)
        {
            if (!list.Contains(itemsManager.GetInfo(ItemID.Hut_Shelter)))
            {
                list.Add(itemsManager.GetInfo(ItemID.Hut_Shelter));
            }

            if (!list.Contains(itemsManager.GetInfo(ItemID.Medium_Bamboo_Shelter)))
            {
                list.Add(itemsManager.GetInfo(ItemID.Medium_Bamboo_Shelter));
            }

            if (!list.Contains(itemsManager.GetInfo(ItemID.Medium_Shelter)))
            {
                list.Add(itemsManager.GetInfo(ItemID.Medium_Shelter));
            }

            if (!list.Contains(itemsManager.GetInfo(ItemID.Small_Bamboo_Shelter)))
            {
                list.Add(itemsManager.GetInfo(ItemID.Small_Bamboo_Shelter));
            }

            if (!list.Contains(itemsManager.GetInfo(ItemID.Bed_Shelter)))
            {
                list.Add(itemsManager.GetInfo(ItemID.Bed_Shelter));
            }
        }

        public static void ShowHUDInfoLog(string itemID, string localizedTextKey)
        {
            Localization localization = GreenHellGame.Instance.GetLocalization();
            ((HUDMessages)hUDManager.GetHUD(typeof(HUDMessages))).AddMessage(localization.Get(localizedTextKey) + "  " + localization.Get(itemID));
        }

        public static void ShowHUDBigInfo(string text, string header, string textureName)
        {
            HUDManager hUDManager = HUDManager.Get();

            HUDBigInfo hudBigInfo = (HUDBigInfo)hUDManager.GetHUD(typeof(HUDBigInfo));
            HUDBigInfoData hudBigInfoData = new HUDBigInfoData
            {
                m_Header = header,
                m_Text = text,
                m_TextureName = textureName,
                m_ShowTime = Time.time
            };
            hudBigInfo.AddInfo(hudBigInfoData);
            hudBigInfo.Show(true);
        }

        private static void EnableCursor(bool enabled = false)
        {
            CursorManager.Get().ShowCursor(enabled, false);
            player = Player.Get();

            if (enabled)
            {
                player.BlockMoves();
                player.BlockRotation();
                player.BlockInspection();
            }
            else
            {
                player.UnblockMoves();
                player.UnblockRotation();
                player.UnblockInspection();
            }
        }
    }
}
