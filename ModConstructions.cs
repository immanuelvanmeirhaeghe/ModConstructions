using Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public static bool HasUnlockedShelters = false;

        public static bool TestRainFXInfoShown = false;

        public bool IsTestRainFxEnabled;

        public bool IsOptionInstantFinishConstructionsActive;

        /// <summary>
        /// ModAPI required security check to enable this mod feature.
        /// </summary>
        /// <returns></returns>
        public bool IsLocalOrHost => ReplTools.AmIMaster() || !ReplTools.IsCoopEnabled();

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
            if (Input.GetKeyDown(KeyCode.Home))
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
            }
            if (!IsOptionInstantFinishConstructionsActive && IsLocalOrHost && IsModConstructionsActive && Input.GetKeyDown(KeyCode.F8))
            {
                ShowHUDBigInfo("Feature disabled in multiplayer!", "Mod Constructions Info", HUDInfoLogTextureType.Count.ToString());
            }
            if (IsTestRainFxEnabled && IsLocalOrHost && IsModConstructionsActive)
            {
                StartRain();
            }
            else
            {
                StopRain();
            }
            UpdateRainTest();
            if (TestRainFXInfoShown)
            {
                ShowHUDBigInfo("Testing rain FX - check beneath roofs!", "ModConstructions Info", HUDInfoLogTextureType.Count.ToString());
            }
        }

        public static void StartRain()
        {
            RainManager.Get().ScenarioStartRain();
        }

        private static void UpdateRainTest()
        {
            if (RainManager.Get().IsRain())
            {
                RainProofing();
                TestRainFXInfoShown = true;
            }
            else
            {
                TestRainFXInfoShown = false;
            }
        }

        public static void StopRain()
        {
            RainManager.Get().ScenarioStopRain();
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
            GUI.Box(new Rect(10f, 10f, 550f, 100f), "ModConstructions UI", GUI.skin.window);

            GUI.Label(new Rect(30f, 30f, 300f, 20f), "Click to unlock all constructions", GUI.skin.label);
            if (GUI.Button(new Rect(350f, 30f, 150f, 20f), "Unlock constructions", GUI.skin.button))
            {
                OnClickUnlockConstructionsButton();
                showUI = false;
                EnableCursor(false);
            }

            GUI.Label(new Rect(30f, 50f, 300f, 20f), "Click to unlock all shelters and beds", GUI.skin.label);
            if (GUI.Button(new Rect(350f, 50f, 150f, 20f), "Unlock shelters", GUI.skin.button))
            {
                OnClickUnlockSheltersButton();
                showUI = false;
                EnableCursor(false);
            }

            GUI.Label(new Rect(30f, 70f, 300f, 20f), "Use F8 to instantly finish constructions", GUI.skin.label);
            IsOptionInstantFinishConstructionsActive = GUI.Toggle(new Rect(350f, 70f, 20f, 20f), IsOptionInstantFinishConstructionsActive, "");

            GUI.Label(new Rect(30f, 90f, 300f, 20f), "Test rain FX", GUI.skin.label);
            IsTestRainFxEnabled = GUI.Toggle(new Rect(350f, 90f, 20f, 20f), IsTestRainFxEnabled, "");
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

        public static void OnClickUnlockSheltersButton()
        {
            try
            {
                UnlockAllShelters();
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{nameof(ModConstructions)}.{nameof(ModConstructions)}:{nameof(OnClickUnlockSheltersButton)}] throws exception: {exc.Message}");
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

        public static void UnlockAllShelters()
        {
            try
            {
                if (!HasUnlockedShelters)
                {
                    List<ItemInfo> list = itemsManager.GetAllInfos().Values.Where(info => info.IsShelter()).ToList();

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

                    foreach (ItemInfo shelterItemInfo in list)
                    {
                        itemsManager.UnlockItemInNotepad(shelterItemInfo.m_ID);
                        itemsManager.UnlockItemInfo(shelterItemInfo.m_ID.ToString());
                        ShowHUDInfoLog(shelterItemInfo.m_ID.ToString(), "HUD_InfoLog_NewEntry");
                    }
                    HasUnlockedShelters = true;
                }
                else
                {
                    ShowHUDBigInfo("All shelters unlocked", "Mod Constructions Info", HUDInfoLogTextureType.Count.ToString());
                }
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{nameof(ModConstructions)}.{nameof(ModConstructions)}:{nameof(UnlockAllShelters)}] throws exception: {exc.Message}");
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
