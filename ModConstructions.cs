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
        private static ModConstructions Instance;

        private static readonly string ModName = nameof(ModConstructions);
        private static readonly float ModScreenTotalWidth = 850f;
        private static readonly float ModScreenMaxHeight = 500f;
        private static readonly float ModScreenMinHeight = 150f;

        private static bool IsMinimized { get; set; } = false;
        public static Rect ModConstructionsScreen = new Rect(Screen.width / 30f, Screen.height / 30f, ModScreenTotalWidth, ModScreenMinHeight);
        private bool ShowUI = false;
        private static ItemsManager LocalItemsManager;
        private static Player LocalPlayer;
        private static HUDManager LocalHUDManager;

        public static Item SelectedItemToDestroy = null;
        public static GameObject SelectedObjectToDestroy = null;
        public static List<ItemInfo> ConstructionItemInfos = new List<ItemInfo>();

        public static string OnlyForSinglePlayerOrHostMessage() => $"Only available for single player or when host. Host can activate using ModManager.";
        public static string AlreadyUnlockedConstructions() => $"All constructions were already unlocked!";
        public static string ItemDestroyedMessage(string item) => $"{item} destroyed!";
        public static string NoItemSelectedMessage() => $"No item selected to destroy!";
        public static string PermissionChangedMessage(string permission) => $"Permission to use mods and cheats in multiplayer was {permission}!";
        public static string HUDBigInfoMessage(string message, Color? headcolor = null)
            => $"<color=#{ (headcolor != null ? ColorUtility.ToHtmlStringRGBA(headcolor.Value) : ColorUtility.ToHtmlStringRGBA(Color.red))  }>{ModName} Info</color>\n{message}";

        public static bool HasUnlockedConstructions { get; private set; }
        public bool InstantFinishConstructionsOption { get; private set; }

        public bool IsModActiveForMultiplayer { get; private set; }
        public bool IsModActiveForSingleplayer => ReplTools.AmIMaster();

        public ModConstructions()
        {
            useGUILayout = true;
            Instance = this;
        }
        public static ModConstructions Get()
        {
            return Instance;
        }

        public void ShowHUDInfoLog(string itemID, string localizedTextKey)
        {
            Localization localization = GreenHellGame.Instance.GetLocalization();
            ((HUDMessages)LocalHUDManager.GetHUD(typeof(HUDMessages))).AddMessage(localization.Get(localizedTextKey) + "  " + localization.Get(itemID));
        }
        public void ShowHUDBigInfo(string text)
        {
            string header = $"{ModName} Info";
            string textureName = HUDInfoLogTextureType.Count.ToString();
            HUDBigInfo hudBigInfo = (HUDBigInfo)LocalHUDManager.GetHUD(typeof(HUDBigInfo));
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
                LocalPlayer.BlockMoves();
                LocalPlayer.BlockRotation();
                LocalPlayer.BlockInspection();
            }
            else
            {
                LocalPlayer.UnblockMoves();
                LocalPlayer.UnblockRotation();
                LocalPlayer.UnblockInspection();
            }
        }

        public void Start()
        {
            ModManager.ModManager.onPermissionValueChanged += ModManager_onPermissionValueChanged;
        }

        private void ModManager_onPermissionValueChanged(bool optionValue)
        {
            IsModActiveForMultiplayer = optionValue;
            ShowHUDBigInfo(
                          (optionValue ?
                            HUDBigInfoMessage(PermissionChangedMessage($"granted"), Color.green)
                            : HUDBigInfoMessage(PermissionChangedMessage($"revoked"), Color.yellow))
                            );
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
            LocalHUDManager = HUDManager.Get();
            LocalItemsManager = ItemsManager.Get();
            LocalPlayer = Player.Get();
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
                ScreenMenuBox();

                UnlockConstructionsBox();

                ModOptionsBox();
            }
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 10000f));
        }

        private void UnlockConstructionsBox()
        {
            using (var horizontalScope = new GUILayout.HorizontalScope(GUI.skin.box))
            {
                GUILayout.Label("All constructions.", GUI.skin.label);
                if (GUILayout.Button("Unlock blueprints", GUI.skin.button))
                {
                    OnClickUnlockConstructionsButton();
                    CloseWindow();
                }
            }
        }

        private void ScreenMenuBox()
        {
            if (GUI.Button(new Rect(ModConstructionsScreen.width - 40f, 0f, 20f, 20f), "-", GUI.skin.button))
            {
                CollapseWindow();
            }

            if (GUI.Button(new Rect(ModConstructionsScreen.width - 20f, 0f, 20f, 20f), "X", GUI.skin.button))
            {
                CloseWindow();
            }
        }

        private void CollapseWindow()
        {
            if (!IsMinimized)
            {
                ModConstructionsScreen.Set(ModConstructionsScreen.x, ModConstructionsScreen.y, ModScreenTotalWidth, 30f);
                IsMinimized = true;
            }
            else
            {
                ModConstructionsScreen.Set(ModConstructionsScreen.x, ModConstructionsScreen.y, ModScreenTotalWidth, ModScreenMinHeight);
                IsMinimized = false;
            }
        }

        private void CloseWindow()
        {
            ShowUI = false;
            EnableCursor(false);
        }

        private void ModOptionsBox()
        {
            if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
            {
                using (var horizontalScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    InstantFinishConstructionsOption = GUILayout.Toggle(InstantFinishConstructionsOption, $"Use F8 to instantly finish any constructions?", GUI.skin.toggle);
                }
            }
            else
            {
                using (var verticalScope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUI.color = Color.yellow;
                    GUILayout.Label(OnlyForSinglePlayerOrHostMessage(), GUI.skin.label);
                    GUI.color = Color.white;
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
                ModAPI.Log.Write($"[{ModName}:{nameof(OnClickUnlockConstructionsButton)}] throws exception:\n{exc.Message}");
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
                        DestroyOnHit(hitInfo);
                    }
                }
                else
                {
                    ShowHUDBigInfo(HUDBigInfoMessage(OnlyForSinglePlayerOrHostMessage(), Color.yellow));
                }
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{ModName}:{nameof(DestroyMouseTarget)}] throws exception:\n{exc.Message}");
            }
        }

        public void DestroyOnHit(RaycastHit hitInfo)
        {
            try
            {
                if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
                {
                    GameObject go = hitInfo.collider.transform.gameObject;
                    if (go != null)
                    {
                        SelectedObjectToDestroy = go;
                        ShowConfirmDestroyDialog();
                    }
                }
                else
                {
                    ShowHUDBigInfo(HUDBigInfoMessage(OnlyForSinglePlayerOrHostMessage(), Color.yellow));
                }
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{ModName}:{nameof(DestroyOnHit)}] throws exception:\n{exc.Message}");
            }
        }

        private void ShowConfirmDestroyDialog()
        {
            EnableCursor(true);
            string description = SelectedObjectToDestroy != null ? SelectedObjectToDestroy.name : $"item";
            YesNoDialog destroyYesNoDialog = GreenHellGame.GetYesNoDialog();
            destroyYesNoDialog.Show(this, DialogWindowType.YesNo, $"{ModName} Info", $"Destroy {description}?", true);
        }

        public void UnlockAllConstructions()
        {
            try
            {
                if (!HasUnlockedConstructions)
                {
                    ConstructionItemInfos = LocalItemsManager.GetAllInfos().Values.Where(info => info.IsConstruction()
                                                                                                                                                                                || info.IsStand()
                                                                                                                                                                                || info.IsWall()
                                                                                                                                                                                || ItemInfo.IsSmoker(info.m_ID)).ToList();
                    if (ConstructionItemInfos != null)
                    {
                        foreach (ItemInfo constructionItemInfo in ConstructionItemInfos)
                        {
                            if (constructionItemInfo.m_ID != ItemID.safezone_totem && constructionItemInfo.m_ID != ItemID.token_stand)
                            {
                                LocalItemsManager.UnlockItemInNotepad(constructionItemInfo.m_ID);
                                LocalItemsManager.UnlockItemInfo(constructionItemInfo.m_ID.ToString());
                                ShowHUDInfoLog(constructionItemInfo.m_ID.ToString(), "HUD_InfoLog_NewEntry");
                            }
                        }
                    }
                    HasUnlockedConstructions = true;
                }
                else
                {
                    ShowHUDBigInfo(AlreadyUnlockedConstructions());
                }
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{ModName}:{nameof(UnlockAllConstructions)}] throws exception:\n{exc.Message}");
            }
        }

        public void OnYesFromDialog()
        {
            DestroySelectedItem();
            EnableCursor(false);
        }

        private void DestroySelectedItem()
        {
            try
            {
                if (SelectedObjectToDestroy != null)
                {
                    ItemInfo selectedItemInfo = LocalItemsManager.GetInfo(SelectedObjectToDestroy.name);
                    SelectedItemToDestroy = selectedItemInfo != null ? selectedItemInfo.m_Item : SelectedObjectToDestroy.GetComponent<Item>();
                    if (SelectedItemToDestroy != null)
                    {
                        if (!SelectedItemToDestroy.IsPlayer() && !SelectedItemToDestroy.IsAI() && !SelectedItemToDestroy.IsHumanAI())
                        {
                            LocalItemsManager.AddItemToDestroy(SelectedItemToDestroy);
                            ShowHUDBigInfo(HUDBigInfoMessage(ItemDestroyedMessage(SelectedItemToDestroy.m_Info.GetNameToDisplayLocalized()), Color.green));
                        }
                    }
                }
                else
                {
                    ShowHUDBigInfo(HUDBigInfoMessage(NoItemSelectedMessage(), Color.yellow));
                }
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{ModName}:{nameof(DestroySelectedItem)}] throws exception:\n{exc.Message}");
            }
        }

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
            EnableCursor(false);
        }
    }
}
