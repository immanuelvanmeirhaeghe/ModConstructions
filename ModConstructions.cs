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
    public class ModConstructions : MonoBehaviour, IYesNoDialogOwner
    {
        private static ModConstructions s_Instance;

        private static readonly string ModName = nameof(ModConstructions);

        public Rect ModConstructionsScreen = new Rect(500f, 500f, 450f, 150f);

        private static ItemsManager itemsManager;

        private static Player player;

        private static HUDManager hUDManager;

        private static Item SelectedItemToDestroy;

        public static List<ItemInfo> ConstructionItemInfos = new List<ItemInfo>();

        public static bool HasUnlockedConstructions { get; private set; }

        public static bool HasUnlockedRestingPlaces { get; private set; }

        private bool showUI = false;

        public bool UseOptionF8 { get; private set; }

        public bool IsModActiveForMultiplayer => FindObjectOfType(typeof(ModManager.ModManager)) != null && ModManager.ModManager.AllowModsForMultiplayer;

        public bool IsModActiveForSingleplayer => ReplTools.AmIMaster();

        public ModConstructions()
        {
            useGUILayout = true;
            s_Instance = this;
        }

        public static ModConstructions Get()
        {
            return s_Instance;
        }

        public void ShowHUDInfoLog(string itemID, string localizedTextKey)
        {
            Localization localization = GreenHellGame.Instance.GetLocalization();
            ((HUDMessages)hUDManager.GetHUD(typeof(HUDMessages))).AddMessage(localization.Get(localizedTextKey) + "  " + localization.Get(itemID));
        }

        public void ShowHUDBigInfo(string text, string header, string textureName)
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

        private void EnableCursor(bool blockPlayer = false)
        {
            CursorManager.Get().ShowCursor(blockPlayer, false);

            if (blockPlayer)
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
                ToggleShowUI();
                if (!showUI)
                {
                    EnableCursor(false);
                }
            }

            if (Input.GetKeyDown(KeyCode.Delete))
            {
                DestroyMouseTarget();
            }
        }

        private void ToggleShowUI()
        {
            showUI = !showUI;
        }

        private void OnGUI()
        {
            if (showUI)
            {
                InitData();
                InitSkinUI();
                InitWindow();
            }
        }

        private void InitData()
        {
            hUDManager = HUDManager.Get();
            itemsManager = ItemsManager.Get();
            player = Player.Get();
        }

        private void InitSkinUI()
        {
            GUI.skin = ModAPI.Interface.Skin;
        }

        private void InitWindow()
        {
            int wid = GetHashCode();
            ModConstructionsScreen = GUILayout.Window(wid, ModConstructionsScreen, InitModConstructionsScreen, $"{ModName}", GUI.skin.window);
        }

        private void InitModConstructionsScreen(int windowID)
        {
            using (var verticalScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                if (GUI.Button(new Rect(430f, 0f, 20f, 20f), "x", GUI.skin.button))
                {
                    CloseWindow();
                }

                using (var horizontalScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label("Construction blueprints", GUI.skin.label);
                    if (GUILayout.Button("Unlock constructions", GUI.skin.button))
                    {
                        OnClickUnlockConstructionsButton();
                        CloseWindow();
                    }
                }

                CreateF8Option();
            }
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 10000f));
        }

        private void CloseWindow()
        {
            showUI = false;
            EnableCursor(false);
        }

        private void CreateF8Option()
        {
            if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
            {
                using (var horizontalScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label("Use F8 to instantly finish", GUI.skin.label);
                    UseOptionF8 = GUILayout.Toggle(UseOptionF8, string.Empty, GUI.skin.toggle);
                }
            }
            else
            {
                using (var verticalScope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label("F8 option", GUI.skin.label);
                    GUILayout.Label("is only for single player or when host", GUI.skin.label);
                    GUILayout.Label("Host can activate using ModManager.", GUI.skin.label);
                }
            }
        }

        private void OnClickUnlockConstructionsButton()
        {
            try
            {
                UnlockAllConstructions();
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{ModName}.{ModName}:{nameof(OnClickUnlockConstructionsButton)}] throws exception: {exc.Message}");
            }
        }

        public void DestroyMouseTarget()
        {
            try
            {
                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hitInfo))
                {
                    GameObject go = hitInfo.collider.transform.gameObject;
                    if (go != null)
                    {
                        Item item = go.GetComponent<Item>();
                        if (item != null)
                        {
                            if (!item.IsPlayer() && !item.IsAI() && !item.IsHumanAI())
                            {
                                SelectedItemToDestroy = item;
                                var deleteYesNo = GreenHellGame.GetYesNoDialog();
                                deleteYesNo.Show(this, Enums.DialogWindowType.YesNo, $"Destroy {item.m_Info.GetNameToDisplayLocalized()}", "Destroy?", true);
                            }
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{ModName}.{ModName}:{nameof(DestroyMouseTarget)}] throws exception: {exc.Message}");
            }
        }

        public void UnlockAllConstructions()
        {
            try
            {
                if (!HasUnlockedConstructions)
                {
                    ConstructionItemInfos = itemsManager.GetAllInfos().Values.Where(info => info.IsConstruction()
                                                                                                                                                                                || info.IsStand()
                                                                                                                                                                                || info.IsWall()
                                                                                                                                                                                || ItemInfo.IsSmoker(info.m_ID)).ToList();
                    if (ConstructionItemInfos != null)
                    {
                        foreach (ItemInfo constructionItemInfo in ConstructionItemInfos)
                        {
                            itemsManager.UnlockItemInNotepad(constructionItemInfo.m_ID);
                            itemsManager.UnlockItemInfo(constructionItemInfo.m_ID.ToString());
                            ShowHUDInfoLog(constructionItemInfo.m_ID.ToString(), "HUD_InfoLog_NewEntry");
                        }
                    }
                    HasUnlockedConstructions = true;
                }
                else
                {
                    ShowHUDBigInfo("All constructions were already unlocked!", $"{ModName} Info", HUDInfoLogTextureType.Count.ToString());
                }
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{ModName}.{ModName}:{nameof(UnlockAllConstructions)}] throws exception: {exc.Message}");
            }
        }

        public void OnYesFromDialog()
        {
            if (SelectedItemToDestroy != null)
            {
                itemsManager.AddItemToDestroy(SelectedItemToDestroy);
                ShowHUDBigInfo($"{SelectedItemToDestroy.m_Info.GetNameToDisplayLocalized()} destroyed!", $"{ModName} Info", HUDInfoLogTextureType.Count.ToString());
            }
        }

        public void OnNoFromDialog()
        {
            SelectedItemToDestroy = null;
        }

        public void OnOkFromDialog()
        {

        }

        public void OnCloseDialog()
        {

        }
    }
}
