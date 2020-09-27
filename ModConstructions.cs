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
    public class ModConstructions : MonoBehaviour, IYesNoDialogOwner
    {
        private static ModConstructions s_Instance;

        private static readonly string ModName = nameof(ModConstructions);

        public Rect ModConstructionsScreen = new Rect(500f, 500f, 450f, 150f);

        private static ItemsManager itemsManager;

        private static Player player;

        private static HUDManager hUDManager;

        private static Item SelectedItemToDestroy;

        public static string OnlyForSinglePlayerOrHostMessage()
            => $"\n<color=#{ColorUtility.ToHtmlStringRGBA(Color.yellow)}>DELETE option</color> is only available for single player or when host.\nHost can activate using <b>ModManager</b>.";

        public static string AlreadyUnlockedConstructions()
             => $"\n<color=#{ColorUtility.ToHtmlStringRGBA(Color.yellow)}>All constructions were already unlocked!</color>";

        public static List<ItemInfo> ConstructionItemInfos = new List<ItemInfo>();

        public static bool HasUnlockedConstructions { get; private set; }

        private bool ShowUI = false;

        public bool UseOptionF8 { get; private set; }

        private bool _isActiveForMultiplayer;
        public bool IsModActiveForMultiplayer
        {
            get => _isActiveForMultiplayer;
            set => _isActiveForMultiplayer = FindObjectOfType(typeof(ModManager.ModManager)) != null && ModManager.ModManager.AllowModsForMultiplayer;
        }

        private bool _isActiveForSingleplayer;
        public bool IsModActiveForSingleplayer
        {
            get => _isActiveForSingleplayer;
            set => _isActiveForSingleplayer = ReplTools.AmIMaster();
        }

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
                if (!ShowUI)
                {
                    InitData();
                    EnableCursor(true);
                }
                ToggleShowUI();
                if (!ShowUI)
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
            ShowUI = !ShowUI;
        }

        private void OnGUI()
        {
            if (ShowUI)
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
                if (GUI.Button(new Rect(430f, 0f, 20f, 20f), "X", GUI.skin.button))
                {
                    CloseWindow();
                }

                using (var horizontalScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label("All constructions.", GUI.skin.label);
                    if (GUILayout.Button("Unlock blueprints", GUI.skin.button))
                    {
                        OnClickUnlockConstructionsButton();
                        CloseWindow();
                    }
                }

                InstantFinishConstructionsOption();
            }
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 10000f));
        }

        private void CloseWindow()
        {
            ShowUI = false;
            EnableCursor(false);
        }

        private void InstantFinishConstructionsOption()
        {
            if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
            {
                using (var horizontalScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    UseOptionF8 = GUILayout.Toggle(UseOptionF8, "Use F8 to instantly finish constructions.", GUI.skin.toggle);
                }
            }
            else
            {
                using (var verticalScope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label("F8 option to instantly finish constructions", GUI.skin.label);
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
                if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
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
                                    EnableCursor(true);
                                    SelectedItemToDestroy = item;
                                    YesNoDialog deleteYesNo = GreenHellGame.GetYesNoDialog();
                                    deleteYesNo.Show(this, DialogWindowType.YesNo, $"{ModName} Info", $"Destroy {item.m_Info.GetNameToDisplayLocalized()}?", false);
                                }
                            }
                        }
                    }
                }
                else
                {
                    ShowHUDBigInfo(OnlyForSinglePlayerOrHostMessage(), $"{ModName} Info", HUDInfoLogTextureType.Count.ToString());
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
                            if (constructionItemInfo.m_ID != ItemID.safezone_totem && constructionItemInfo.m_ID != ItemID.token_stand )
                            {
                                itemsManager.UnlockItemInNotepad(constructionItemInfo.m_ID);
                                itemsManager.UnlockItemInfo(constructionItemInfo.m_ID.ToString());
                                ShowHUDInfoLog(constructionItemInfo.m_ID.ToString(), "HUD_InfoLog_NewEntry");
                            }
                        }
                    }
                    HasUnlockedConstructions = true;
                }
                else
                {
                    ShowHUDBigInfo(AlreadyUnlockedConstructions(), $"{ModName} Info", HUDInfoLogTextureType.Count.ToString());
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
                ShowHUDBigInfo(
                    ItemDestroyedMessage(SelectedItemToDestroy),
                    $"{ModName} Info",
                    HUDInfoLogTextureType.Count.ToString());
            }
            EnableCursor(false);
        }

        private string ItemDestroyedMessage(Item selectedItemToDestroy) => $"<color=#{ColorUtility.ToHtmlStringRGBA(Color.red)}>System</color>:\n{selectedItemToDestroy.m_Info.GetNameToDisplayLocalized()} destroyed!";

        public void OnNoFromDialog()
        {
            SelectedItemToDestroy = null;
            EnableCursor(false);
        }

        public void OnOkFromDialog()
        {
            OnYesFromDialog();
        }

        public void OnCloseDialog()
        {
            SelectedItemToDestroy = null;
            EnableCursor(false);
        }
    }
}
