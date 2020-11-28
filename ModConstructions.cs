using Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ModConstructions
{
    public enum MessageType
    {
        Info,
        Warning,
        Error
    }

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
        private static readonly float ModScreenTotalWidth = 500f;
        private static readonly float ModScreenTotalHeight = 150f;
        private static readonly float ModScreenMinHeight = 30f;
        private static readonly float ModScreenMaxHeight = 180f;

        private static bool IsMinimized { get; set; } = false;
        public static Rect ModConstructionsScreen = new Rect(Screen.width / ModScreenMinHeight, Screen.height / ModScreenMinHeight, ModScreenTotalWidth, ModScreenTotalHeight);
        private bool ShowUI = false;
        private static ItemsManager LocalItemsManager;
        private static Player LocalPlayer;
        private static HUDManager LocalHUDManager;

        public static Item SelectedItemToDestroy = null;
        public static GameObject SelectedGameObjectToDestroy = null;
        public static string SelectedGameObjectToDestroyName = string.Empty;
        public static List<string> DestroyableObjectNames { get; set; } = new List<string> {
                                                                                                                                        "tree", "plant", "leaf", "stone", "seat", "bag", "beam", "corrugated", "anaconda",
                                                                                                                                        "metal", "board", "cardboard", "plank", "plastic", "tarp", "oil", "sock",
                                                                                                                                        "cartel", "military", "tribal", "village", "ayahuasca", "gas", "boat", "ship",
                                                                                                                                        "bridge", "chair", "stove", "barrel", "tank", "jerrycan", "microwave",
                                                                                                                                        "sprayer", "shelf", "wind", "bottle", "trash", "lab", "table", "diving",
                                                                                                                                        "roof", "floor", "hull", "frame", "cylinder", "wire", "wiretap"
                                                                                                                                };
        public static List<ItemInfo> ConstructionItemInfos = new List<ItemInfo>();

        public static string OnlyForSinglePlayerOrHostMessage() => $"Only available for single player or when host. Host can activate using ModManager.";
        public static string AlreadyUnlockedBlueprints() => $"All blueprints were already unlocked!";
        public static string ItemDestroyedMessage(string item) => $"{item} destroyed!";
        public static string ItemNotSelectedMessage() => $"Not any item selected to destroy!";
        public static string ItemNotDestroyedMessage(string item) => $"{item} cannot be destroyed!";
        public static string PermissionChangedMessage(string permission) => $"Permission to use mods and cheats in multiplayer was {permission}!";
        public static string HUDBigInfoMessage(string message, MessageType messageType, Color? headcolor = null)
            => $"<color=#{ (headcolor != null ? ColorUtility.ToHtmlStringRGBA(headcolor.Value) : ColorUtility.ToHtmlStringRGBA(Color.red))  }>{messageType}</color>\n{message}";

        public static bool HasUnlockedConstructions { get; private set; }
        public bool InstantFinishConstructionsOption { get; private set; }
        public bool DestroyTargetOption { get; private set; }

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
            HUDBigInfoData.s_Duration = 2f;
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
                            HUDBigInfoMessage(PermissionChangedMessage($"granted"), MessageType.Info, Color.green)
                            : HUDBigInfoMessage(PermissionChangedMessage($"revoked"), MessageType.Info, Color.yellow))
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
                DestroyTarget();
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
            using (var modContentScope = new GUILayout.VerticalScope(GUI.skin.box, GUILayout.ExpandHeight(true), GUILayout.MinHeight(ModScreenMinHeight), GUILayout.MaxHeight(ModScreenMaxHeight)))
            {
                ScreenMenuBox();
                if (!IsMinimized)
                {
                    UnlockBlueprintsBox();
                    ModOptionsBox();
                }
            }
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 10000f));
        }

        private void UnlockBlueprintsBox()
        {
            using (var constructionsScope = new GUILayout.HorizontalScope(GUI.skin.box))
            {
                GUILayout.Label("Click to unlock all constructions info: ", GUI.skin.label);
                if (GUILayout.Button("Unlock blueprints", GUI.skin.button))
                {
                    OnClickUnlockBlueprintsButton();
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
                ModConstructionsScreen.Set(ModConstructionsScreen.x, Screen.height - ModScreenMinHeight, ModScreenTotalWidth, ModScreenMinHeight);
                IsMinimized = true;
            }
            else
            {
                ModConstructionsScreen.Set(ModConstructionsScreen.x, Screen.height / ModScreenMinHeight, ModScreenTotalWidth, ModScreenTotalHeight);
                IsMinimized = false;
            }
            InitWindow();
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
                using (var optionScope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    InstantFinishConstructionsOption = GUILayout.Toggle(InstantFinishConstructionsOption, $"Use [F8] to instantly finish any constructions?", GUI.skin.toggle);
                    DestroyTargetOption = GUILayout.Toggle(DestroyTargetOption, $"Use [DELETE] to destroy target?", GUI.skin.toggle);
                }
            }
            else
            {
                using (var infoScope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUI.color = Color.yellow;
                    GUILayout.Label(OnlyForSinglePlayerOrHostMessage(), GUI.skin.label);
                    GUI.color = Color.white;
                }
            }
        }

        private void OnClickUnlockBlueprintsButton()
        {
            try
            {
                UnlockAllConstructions();
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{ModName}:{nameof(OnClickUnlockBlueprintsButton)}] throws exception:\n{exc.Message}");
            }
        }

        public void DestroyTarget()
        {
            try
            {
                if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
                {
                    if (DestroyTargetOption)
                    {
                        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hitInfo))
                        {
                            DestroyOnHit(hitInfo);
                        }
                    }
                }
                else
                {
                    ShowHUDBigInfo(HUDBigInfoMessage(OnlyForSinglePlayerOrHostMessage(), MessageType.Warning, Color.yellow));
                }
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{ModName}:{nameof(DestroyTarget)}] throws exception:\n{exc.Message}");
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
                        SelectedGameObjectToDestroy = go.gameObject;
                        ShowConfirmDestroyDialog();
                    }
                }
                else
                {
                    ShowHUDBigInfo(HUDBigInfoMessage(OnlyForSinglePlayerOrHostMessage(), MessageType.Warning, Color.yellow));
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
            string description = $"Are you sure you want to destroy selected { (SelectedGameObjectToDestroy != null ? SelectedGameObjectToDestroy.name : "item") }?";
            YesNoDialog destroyYesNoDialog = GreenHellGame.GetYesNoDialog();
            destroyYesNoDialog.Show(this, DialogWindowType.YesNo, $"{ModName} Info", description, true);
        }

        public void UnlockAllConstructions()
        {
            try
            {
                if (!HasUnlockedConstructions)
                {
                    ConstructionItemInfos = LocalItemsManager.GetAllInfos().Values.Where(info => info.IsConstruction()).ToList();
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
                    ShowHUDBigInfo(HUDBigInfoMessage(AlreadyUnlockedBlueprints(), MessageType.Warning, Color.yellow));
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
                if (SelectedGameObjectToDestroy != null)
                {
                    SelectedItemToDestroy = SelectedGameObjectToDestroy.GetComponent<Item>();
                    SelectedGameObjectToDestroyName = SelectedItemToDestroy != null && SelectedItemToDestroy.m_Info != null
                                                                                                    ? SelectedItemToDestroy.m_Info.GetNameToDisplayLocalized()
                                                                                                    : GreenHellGame.Instance.GetLocalization().Get(SelectedGameObjectToDestroy.name);

                    if (SelectedItemToDestroy != null || IsDestroyable(SelectedGameObjectToDestroy))
                    {
                        if (SelectedItemToDestroy != null && !SelectedItemToDestroy.IsPlayer() && !SelectedItemToDestroy.IsAI() && !SelectedItemToDestroy.IsHumanAI())
                        {
                            LocalItemsManager.AddItemToDestroy(SelectedItemToDestroy);
                        }
                        else
                        {
                            Destroy(SelectedGameObjectToDestroy);
                        }
                        ShowHUDBigInfo(HUDBigInfoMessage(ItemDestroyedMessage(SelectedGameObjectToDestroyName), MessageType.Info, Color.green));
                    }
                    else
                    {
                        ShowHUDBigInfo(HUDBigInfoMessage(ItemNotDestroyedMessage(SelectedGameObjectToDestroyName), MessageType.Error, Color.red));
                    }
                }
                else
                {
                    ShowHUDBigInfo(HUDBigInfoMessage(ItemNotSelectedMessage(), MessageType.Warning, Color.yellow));
                }
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{ModName}:{nameof(DestroySelectedItem)}] throws exception:\n{exc.Message}");
            }
        }

        private bool IsDestroyable(GameObject go)
        {
            try
            {
                if (go == null || string.IsNullOrEmpty(go.name))
                {
                    return false;
                }
                return DestroyableObjectNames.Any(destroyableObjectName => go.name.ToLower().Contains(destroyableObjectName));
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{ModName}:{nameof(IsDestroyable)}] throws exception:\n{exc.Message}");
                return false;
            }
        }

        public void OnNoFromDialog()
        {
            SelectedGameObjectToDestroy = null;
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
