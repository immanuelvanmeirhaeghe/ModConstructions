using Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ModConstructions
{
    /// <summary>
    /// ModConstructions is a mod for Green Hell that allows a player to unlock all constructions, shelters and beds.
    /// It also gives the player the possibility to instantly finish any ongoing constructions.
    /// (only in single player mode - Use ModManager for multiplayer).
    /// Enable the mod UI by pressing Home.
    /// </summary>
    public class ModConstructions : MonoBehaviour
    {
        private static ModConstructions s_Instance;

        private bool showUI = false;

        private static ItemsManager itemsManager;

        private static Player player;

        private static HUDManager hUDManager;

        private static List<ItemInfo> m_UnlockedConstructionsItemInfos = new List<ItemInfo>();
        public static bool HasUnlockedConstructions { get; private set; }

        private static List<ItemInfo> m_UnlockedSheltersItemInfos = new List<ItemInfo>();
        public static bool HasUnlockedRestingPlaces { get; private set; }

        public static bool TestRainFXInfoShown { get; private set; }
        public static bool TestRainFxEnabled { get; private set; }

        public bool UseOptionF8 { get; private set; }

        /// <summary>
        /// ModAPI required security check to enable this mod feature for multiplayer.
        /// See <see cref="ModManager"/> for implementation.
        /// Based on request in chat: use  !requestMods in chat as client to request the host to activate mods for them.
        /// </summary>
        /// <returns>true if enabled, else false</returns>
        public bool IsModActiveForMultiplayer => FindObjectOfType(typeof(ModManager.ModManager)) != null ? ModManager.ModManager.AllowModsForMultiplayer : false;

        public bool IsModActiveForSingleplayer => ReplTools.AmIMaster();

        public ModConstructions()
        {
            s_Instance = this;
        }

        public static ModConstructions Get()
        {
            return s_Instance;
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

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Home))
            {
                if (!showUI)
                {
                    InitData();

                    EnableCursor(true);
                }
                // toggle menu
                showUI = !showUI;
                if (!showUI)
                {
                    EnableCursor(false);
                }

                //if (TestRainFxEnabled)
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
                InitSkinUI();
                InitModUI();
            }
        }

        private static void InitData()
        {
            hUDManager = HUDManager.Get();
            itemsManager = ItemsManager.Get();
            player = Player.Get();
        }

        private static void InitSkinUI()
        {
            GUI.skin = ModAPI.Interface.Skin;
        }

        private void InitModUI()
        {
            GUI.Box(new Rect(500f, 500f, 450f, 150f), "ModConstructions UI - Press HOME to open/close", GUI.skin.window);

            GUI.Label(new Rect(520f, 520f, 200f, 20f), "Click to unlock all constructions", GUI.skin.label);
            if (GUI.Button(new Rect(770f, 520f, 150f, 20f), "Unlock constructions", GUI.skin.button))
            {
                OnClickUnlockConstructionsButton();
                showUI = false;
                EnableCursor(false);
            }

            //GUI.Label(new Rect(520f, 580f, 200f, 20f), "Test rain FX", GUI.skin.label);
            //TestRainFxEnabled = GUI.Toggle(new Rect(770f, 580f, 20f, 20f), TestRainFxEnabled, "");

            CreateF8Option();

            if (GUI.Button(new Rect(770f, 580f, 150f, 20f), "CANCEL", GUI.skin.button))
            {
                showUI = false;
                EnableCursor(false);
            }
        }

        private void CreateF8Option()
        {
            if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
            {
                GUI.Label(new Rect(520f, 540f, 200f, 20f), "Use F8 to instantly finish", GUI.skin.label);
                UseOptionF8 = GUI.Toggle(new Rect(770f, 540f, 20f, 20f), UseOptionF8, "");
            }
            else
            {
                GUI.Label(new Rect(520f, 540f, 330f, 20f), "Use F8 to instantly to finish any constructions", GUI.skin.label);
                GUI.Label(new Rect(520f, 560f, 330f, 20f), "is only for single player or when host", GUI.skin.label);
                GUI.Label(new Rect(520f, 580f, 330f, 20f), "Host can activate using ModManager.", GUI.skin.label);
            }
        }

        private static void OnClickUnlockConstructionsButton()
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

        public static void UnlockAllConstructions()
        {
            try
            {
                if (!HasUnlockedConstructions)
                {
                    m_UnlockedConstructionsItemInfos = itemsManager.GetAllInfos().Values.Where(info => info.IsConstruction()).ToList();
                    foreach (ItemInfo constructionItemInfo in m_UnlockedConstructionsItemInfos)
                    {
                        itemsManager.UnlockItemInNotepad(constructionItemInfo.m_ID);
                        itemsManager.UnlockItemInfo(constructionItemInfo.m_ID.ToString());
                        ShowHUDInfoLog(constructionItemInfo.m_ID.ToString(), "HUD_InfoLog_NewEntry");
                    }
                    HasUnlockedConstructions = true;
                }
                else
                {
                    ShowHUDBigInfo("All constructions were already unlocked!", "ModConstructions Info", HUDInfoLogTextureType.Count.ToString());
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
    }
}
