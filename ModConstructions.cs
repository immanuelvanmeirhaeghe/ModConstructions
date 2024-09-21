using Enums;
using ModConstructions.Data.Enums;
using ModConstructions.Managers;
using ModConstructions.Data.Interfaces;
using ModConstructions.Data.Modding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEngine;
using UnityEngine.UI;

namespace ModConstructions
{
    /// <summary>
    /// ModConstructions is a mod for Green Hell that allows a player to unlock all construction blueprints.
    /// It also gives the player the possibility to instantly finish any ongoing constructions and build anywhere.
    /// Press Alpha8 (default) or the key configurable in ModAPI to open the mod screen.
    /// When enabled, press KeypadMinus (default) or the key configurable in ModAPI to delete mouse target.
    /// </summary>
    public class ModConstructions : MonoBehaviour, IYesNoDialogOwner
    {
        private static ModConstructions Instance;
        private static readonly string RuntimeConfigurationFile = Path.Combine(Application.dataPath.Replace("GH_Data", "Mods"), "RuntimeConfiguration.xml");
        private static readonly string ModName = nameof(ModConstructions);

        public string ModConstructionsScreenTitle = $"{ModName} created by [Dragon Legion] Immaanuel#4300";

        private static float ModConstructionsScreenTotalWidth { get; set; } = 700f;
        private static float ModConstructionsScreenTotalHeight { get; set; } = 500f;
        private static float ModConstructionsScreenMinWidth { get; set; } = 700f;
        private static float ModConstructionsScreenMaxWidth { get; set; } = Screen.width;
        private static float ModConstructionsScreenMinHeight { get; set; } = 50f;
        private static float ModConstructionsScreenMaxHeight { get; set; } = Screen.height;
        private static float ModConstructionsScreenStartPositionX { get; set; } = Screen.width / 2f;
        private static float ModConstructionsScreenStartPositionY { get; set; } = Screen.width / 2f;
        private bool IsModConstructionsScreenMinimized { get; set; } = false;
        private static int ModConstructionsScreenId { get; set; }
        private static Rect ModConstructionsScreen = new Rect(ModConstructionsScreenStartPositionX, ModConstructionsScreenStartPositionY, ModConstructionsScreenTotalWidth, ModConstructionsScreenTotalHeight);
        private KeyCode ShortcutKey { get; set; } = KeyCode.Alpha8;
        private KeyCode DeleteShortcutKey { get; set; } = KeyCode.KeypadMinus;
        private bool ShowModConstructionsScreen { get; set; } = false;
               
        private static ItemsManager LocalItemsManager;
        private static Player LocalPlayer;
        private static HUDManager LocalHUDManager;
        private static StylingManager LocalStylingManager;
        private static CursorManager LocalCursorManager;

        public static Item SelectedItemToDestroy = null;
        public static GameObject SelectedGameObjectToDestroy = null;
        public static string SelectedGameObjectToDestroyName = string.Empty;
        public static List<ItemInfo> ConstructionItemInfos = new List<ItemInfo>();

        public static string AlreadyUnlockedBlueprints()
            => $"All blueprints were already unlocked!";
        public static string ItemDestroyedMessage(string item)
            => $"{item} destroyed!";
        public static string ItemNotSelectedMessage()
            => $"Not any item selected to destroy!";
        public static string ItemNotDestroyedMessage(string item)
            => $"{item} cannot be destroyed!";
       
        public bool HasUnlockedConstructions { get; set; } = false;
        public bool InstantBuildOption { get; set; } = false;
        public bool DestroyTargetOption { get; set; } = false;
        public bool IsModActiveForMultiplayer { get; private set; } = false;
        public bool IsModActiveForSingleplayer => ReplTools.AmIMaster();

        public IConfigurableMod SelectedMod { get; set; } = default;
        public Vector2 ModConstructionsInfoScrollViewPosition { get; set; } = default;
        public bool ShowModConstructionsInfo { get; set; } = false;

        public static bool IsModEnabled => Get().IsModActiveForSingleplayer || Get().IsModActiveForMultiplayer;
        public static bool InstantBuildEnabled { get; set; } = false;

        private string OnlyForSinglePlayerOrHostMessage()
                     => "Only available for single player or when host. Host can activate using ModManager.";
        private string PermissionChangedMessage(string permission, string reason)
            => $"Permission to use mods and cheats in multiplayer was {permission} because {reason}.";
        private string HUDBigInfoMessage(string message, MessageType messageType, Color? headcolor = null)
            => $"<color=#{(headcolor != null ? ColorUtility.ToHtmlStringRGBA(headcolor.Value) : ColorUtility.ToHtmlStringRGBA(Color.red))}>{messageType}</color>\n{message}";

        private void ModManager_onPermissionValueChanged(bool optionValue)
        {
            string reason = optionValue ? "the game host allowed usage" : "the game host did not allow usage";
            IsModActiveForMultiplayer = optionValue;

            ShowHUDBigInfo(
                          (optionValue ?
                            HUDBigInfoMessage(PermissionChangedMessage($"granted", $"{reason}"), MessageType.Info, Color.green)
                            : HUDBigInfoMessage(PermissionChangedMessage($"revoked", $"{reason}"), MessageType.Info, Color.yellow))
                            );
        }

        public void ShowHUDBigInfo(string text, float duration = 3f)
        {
            string header = $"{ModName} Info";
            string textureName = HUDInfoLogTextureType.Count.ToString();
            HUDBigInfo obj = (HUDBigInfo)LocalHUDManager.GetHUD(typeof(HUDBigInfo));
            HUDBigInfoData.s_Duration = duration;
            HUDBigInfoData data = new HUDBigInfoData
            {
                m_Header = header,
                m_Text = text,
                m_TextureName = textureName,
                m_ShowTime = Time.time
            };
            obj.AddInfo(data);
            obj.Show(show: true);
        }

        public void ShowHUDInfoLog(string itemID, string localizedTextKey)
        {
            Localization localization = GreenHellGame.Instance.GetLocalization();
            var messages = ((HUDMessages)LocalHUDManager.GetHUD(typeof(HUDMessages)));
            messages.AddMessage($"{localization.Get(localizedTextKey)}  {localization.Get(itemID)}");
        }

        protected virtual void Awake()
        {
            Instance = this;
        }

        protected virtual void OnDestroy()
        {
            Instance = null;
        }

        protected virtual void Start()
        {
            ModManager.ModManager.onPermissionValueChanged += ModManager_onPermissionValueChanged;
            InitData();
            ShortcutKey = GetShortcutKey(nameof(ShortcutKey));
            DeleteShortcutKey = GetShortcutKey(nameof (DeleteShortcutKey));
        }

        public ModConstructions()
        {
            useGUILayout = true;
            Instance = this;
        }

        public static ModConstructions Get()
        {
            return Instance;
        }

        private void EnableCursor(bool blockPlayer = false)
        {
            LocalCursorManager.ShowCursor(blockPlayer, false);

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

        public KeyCode GetShortcutKey(string buttonID)
        {
            var ConfigurableModList = GetModList();
            if (ConfigurableModList != null && ConfigurableModList.Count > 0)
            {
                SelectedMod = ConfigurableModList.Find(cfgMod => cfgMod.ID == ModName);
                return SelectedMod.ConfigurableModButtons.Find(cfgButton => cfgButton.ID == buttonID).ShortcutKey;
            }
            else
            {
                return KeyCode.Keypad8;
            }
        }

        private List<IConfigurableMod> GetModList()
        {
            List<IConfigurableMod> modList = new List<IConfigurableMod>();
            try
            {
                if (File.Exists(RuntimeConfigurationFile))
                {
                    using (XmlReader configFileReader = XmlReader.Create(new StreamReader(RuntimeConfigurationFile)))
                    {
                        while (configFileReader.Read())
                        {
                            configFileReader.ReadToFollowing("Mod");
                            do
                            {
                                string gameID = GameID.GreenHell.ToString();
                                string modID = configFileReader.GetAttribute(nameof(IConfigurableMod.ID));
                                string uniqueID = configFileReader.GetAttribute(nameof(IConfigurableMod.UniqueID));
                                string version = configFileReader.GetAttribute(nameof(IConfigurableMod.Version));

                                var configurableMod = new ConfigurableMod(gameID, modID, uniqueID, version);

                                configFileReader.ReadToDescendant("Button");
                                do
                                {
                                    string buttonID = configFileReader.GetAttribute(nameof(IConfigurableModButton.ID));
                                    string buttonKeyBinding = configFileReader.ReadElementContentAsString();

                                    configurableMod.AddConfigurableModButton(buttonID, buttonKeyBinding);

                                } while (configFileReader.ReadToNextSibling("Button"));

                                if (!modList.Contains(configurableMod))
                                {
                                    modList.Add(configurableMod);
                                }

                            } while (configFileReader.ReadToNextSibling("Mod"));
                        }
                    }
                }
                return modList;
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(GetModList));
                modList = new List<IConfigurableMod>();
                return modList;
            }
        }

        private void HandleException(Exception exc, string methodName)
        {
            string info = $"[{ModName}:{methodName}] throws exception:\n{exc}";
            ModAPI.Log.Write(info);
            ShowHUDBigInfo(HUDBigInfoMessage(exc.Message, MessageType.Error, Color.red));
        }

        protected virtual void Update()
        {
            if (Input.GetKeyDown(ShortcutKey))
            {
                if (!ShowModConstructionsScreen)
                {
                    InitData();
                    EnableCursor(true);
                }
                ToggleShowUI(0);
                if (!ShowModConstructionsScreen)
                {
                    EnableCursor(false);
                }
            }

            if (Input.GetKeyDown(DeleteShortcutKey))
            {
                DestroyTarget();
            }
        }

        private void ToggleShowUI(int controlId)
        {
            switch (controlId)
            {
                case 0:
                    ShowModConstructionsScreen = !ShowModConstructionsScreen;
                    return;
                case 3:
                    ShowModConstructionsInfo = !ShowModConstructionsInfo;
                    return;          
                default:                  
                    ShowModConstructionsInfo = !ShowModConstructionsInfo;
                    ShowModConstructionsScreen = !ShowModConstructionsScreen;
                    return;
            }
        }

        protected virtual void OnGUI()
        {
            if (ShowModConstructionsScreen)
            {
                InitData();
                InitSkinUI();
                ShowModConstructionsWindow();
            }
        }

        protected virtual void InitData()
        {
            LocalCursorManager = CursorManager.Get();
            LocalHUDManager = HUDManager.Get();
            LocalItemsManager = ItemsManager.Get();
            LocalPlayer = Player.Get();
            LocalStylingManager = StylingManager.Get();            
        }

        private void InitSkinUI()
        {
            GUI.skin = ModAPI.Interface.Skin;
        }

        private void ShowModConstructionsWindow()
        {
            ModConstructionsScreenId = GetHashCode();            
            ModConstructionsScreen = GUILayout.Window(ModConstructionsScreenId, ModConstructionsScreen,
                                                      InitModConstructionsScreen, ModConstructionsScreenTitle,
                                                      GUI.skin.window, GUILayout.ExpandWidth(true),
                                                      GUILayout.MinWidth(ModConstructionsScreenMinWidth),
                                                      GUILayout.MaxWidth(ModConstructionsScreenMaxWidth),
                                                      GUILayout.ExpandHeight(true),
                                                      GUILayout.MinHeight(ModConstructionsScreenMinHeight),
                                                      GUILayout.MaxHeight(ModConstructionsScreenMaxHeight));
        }

        private void InitModConstructionsScreen(int windowID)
        {
            ModConstructionsScreenStartPositionX = ModConstructionsScreen.x;
            ModConstructionsScreenStartPositionY = ModConstructionsScreen.y;
            ModConstructionsScreenTotalWidth = ModConstructionsScreen.width;

            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                ModConstructionsScreenMenuBox();

                if (!IsModConstructionsScreenMinimized)
                {
                    ModConstructionsManagerBox();

                    ConstructionsManagerBox();
                    
                }

            }
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 10000f));
        }

        private void ModConstructionsScreenMenuBox()
        {
            string CollapseButtonText = IsModConstructionsScreenMinimized ? "O" : "-";
            if (GUI.Button(new Rect(ModConstructionsScreen.width - 40f, 0f, 20f, 20f),CollapseButtonText, GUI.skin.button))
            {
                CollapseModConstructionsWindow();
            }

            if (GUI.Button(new Rect(ModConstructionsScreen.width - 20f, 0f, 20f, 20f), "X", GUI.skin.button))
            {
                CloseWindow();
            }
        }

        private void CollapseModConstructionsWindow()
        {
            if (!IsModConstructionsScreenMinimized)
            {
                ModConstructionsScreen = new Rect(ModConstructionsScreen.x, ModConstructionsScreen.y, ModConstructionsScreenTotalWidth, ModConstructionsScreenMinHeight);
                IsModConstructionsScreenMinimized = true;
            }
            else
            {
                ModConstructionsScreen = new Rect(ModConstructionsScreen.x, ModConstructionsScreen.y, ModConstructionsScreenTotalWidth, ModConstructionsScreenTotalHeight);
                IsModConstructionsScreenMinimized = false;
            }
            ShowModConstructionsWindow();
        }

        private void CloseWindow()
        {
            ShowModConstructionsScreen = false;
            EnableCursor(false);
        }

        private void ModConstructionsManagerBox()
        {
            if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
            {
                using (new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{ModName} Manager", LocalStylingManager.ColoredHeaderLabel(Color.yellow));
                    GUILayout.Label($"{ModName} Options", LocalStylingManager.ColoredSubHeaderLabel(Color.yellow));

                    if (GUILayout.Button($"Mod Info", GUI.skin.button))
                    {
                        ToggleShowUI(3);
                    }
                    if (ShowModConstructionsInfo)
                    {
                        ModConstructionsInfoBox();
                    }

                    MultiplayerOptionBox();

                    ModShortcutsInfoBox();

                    UnlockBlueprintsBox();

                }
            }
            else
            {
                OnlyForSingleplayerOrWhenHostBox();
            }
        }

        private void UnlockBlueprintsBox()
        {
            if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
            {
                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label("Click to unlock all constructions info: ", LocalStylingManager.FormFieldNameLabel);
                    if (GUILayout.Button("Unlock blueprints", GUI.skin.button, GUILayout.Width(150f)))
                    {
                        OnClickUnlockBlueprintsButton();
                    }
                }
            }
            else
            {
                OnlyForSingleplayerOrWhenHostBox();
            }
        }

        private void ConstructionsManagerBox()
        {
            try
            {
                using (new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label($"Constructions Manager", LocalStylingManager.ColoredHeaderLabel(Color.yellow));
                    GUILayout.Label($"Constructions Options", LocalStylingManager.ColoredSubHeaderLabel(Color.yellow));

                    InstantBuildOptionBox();

                    DestroyTargetOptionBox();
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(ConstructionsManagerBox));
            }
        }

        private void DestroyTargetOptionBox()
        {
            bool _destroyTargetOption = DestroyTargetOption;
            DestroyTargetOption = GUILayout.Toggle(DestroyTargetOption, $"Use [{DeleteShortcutKey}] to destroy target?", GUI.skin.toggle);
            if (_destroyTargetOption != DestroyTargetOption)
            {
                ShowHUDBigInfo(HUDBigInfoMessage($"Destroy target with [{DeleteShortcutKey}] has been {(DestroyTargetOption ? "enabled" : "disabled")} ", MessageType.Info, Color.green));
            }
        }

        private void InstantBuildOptionBox()
        {
            try
            {
                bool _instantBuildOption = Cheats.m_InstantBuild;
                if (Cheats.m_InstantBuild)
                {
                    InstantBuildEnabled = true;
                }
                else
                {
                    InstantBuildEnabled = false;
                }
                Cheats.m_InstantBuild = GUILayout.Toggle(Cheats.m_InstantBuild, $"Instantly build and finish any constructions?", GUI.skin.toggle);

                if (_instantBuildOption != Cheats.m_InstantBuild)
                {
                    if (Cheats.m_InstantBuild)
                    {
                        InstantBuildEnabled = true;
                    }
                    else
                    {
                        InstantBuildEnabled = false;
                    }
                    ShowHUDBigInfo(HUDBigInfoMessage($"Mod is {(IsModEnabled ? "enabled" : "disabled")}\nInstant build has been {(InstantBuildEnabled ? "enabled" : "disabled")} ", MessageType.Info, Color.green));
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(InstantBuildOptionBox));
            }
        }

        private void OnlyForSingleplayerOrWhenHostBox()
        {
            using (new GUILayout.HorizontalScope(GUI.skin.box))
            {
                GUILayout.Label(OnlyForSinglePlayerOrHostMessage(), LocalStylingManager.ColoredCommentLabel(Color.yellow));
            }
        }

        private void ModConstructionsInfoBox()
        {
            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                ModConstructionsInfoScrollViewPosition = GUILayout.BeginScrollView(ModConstructionsInfoScrollViewPosition, GUI.skin.scrollView, GUILayout.MinHeight(150f));

                GUILayout.Label("Mod Info", LocalStylingManager.ColoredSubHeaderLabel(Color.cyan));

                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(IConfigurableMod.GameID)}:", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"{SelectedMod.GameID}", LocalStylingManager.FormFieldValueLabel);
                }
                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(IConfigurableMod.ID)}:", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"{SelectedMod.ID}", LocalStylingManager.FormFieldValueLabel);
                }
                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(IConfigurableMod.UniqueID)}:", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"{SelectedMod.UniqueID}", LocalStylingManager.FormFieldValueLabel);
                }
                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(IConfigurableMod.Version)}:", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"{SelectedMod.Version}", LocalStylingManager.FormFieldValueLabel);
                }

                GUILayout.Label("Buttons Info", LocalStylingManager.ColoredSubHeaderLabel(Color.cyan));

                foreach (var configurableModButton in SelectedMod.ConfigurableModButtons)
                {
                    using (new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label($"{nameof(IConfigurableModButton.ID)}:", LocalStylingManager.FormFieldNameLabel);
                        GUILayout.Label($"{configurableModButton.ID}", LocalStylingManager.FormFieldValueLabel);
                    }
                    using (new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label($"{nameof(IConfigurableModButton.KeyBinding)}:", LocalStylingManager.FormFieldNameLabel);
                        GUILayout.Label($"{configurableModButton.KeyBinding}", LocalStylingManager.FormFieldValueLabel);
                    }
                }

                GUILayout.EndScrollView();
            }
        }

        private void MultiplayerOptionBox()
        {
            try
            {
                using (new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label("Multiplayer Options", LocalStylingManager.ColoredSubHeaderLabel(Color.yellow));

                    string multiplayerOptionMessage = string.Empty;
                    if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
                    {
                        if (IsModActiveForSingleplayer)
                        {
                            multiplayerOptionMessage = $"you are the game host";
                        }
                        if (IsModActiveForMultiplayer)
                        {
                            multiplayerOptionMessage = $"the game host allowed usage";
                        }
                        GUILayout.Label(PermissionChangedMessage($"granted", multiplayerOptionMessage), LocalStylingManager.ColoredFieldValueLabel(Color.green));
                    }
                    else
                    {
                        if (!IsModActiveForSingleplayer)
                        {
                            multiplayerOptionMessage = $"you are not the game host";
                        }
                        if (!IsModActiveForMultiplayer)
                        {
                            multiplayerOptionMessage = $"the game host did not allow usage";
                        }
                        GUILayout.Label(PermissionChangedMessage($"revoked", $"{multiplayerOptionMessage}"), LocalStylingManager.ColoredFieldValueLabel(Color.yellow));
                    }
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(MultiplayerOptionBox));
            }
        }

        private void ModShortcutsInfoBox()
        {
            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.Label("Mod shortcut options", LocalStylingManager.ColoredSubHeaderLabel(Color.yellow));

                GUILayout.Label($"To destroy the target on mouse pointer, press [{DeleteShortcutKey}]", LocalStylingManager.TextLabel);
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
                HandleException(exc, nameof(OnClickUnlockBlueprintsButton));
            }
        }

        private void DestroyTarget()
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
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(DestroyTarget));
            }
        }

        public void DestroyOnHit(RaycastHit hitInfo)
        {
            try
            {
                if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
                {
                    if (DestroyTargetOption)
                    {
                        SelectedGameObjectToDestroy = hitInfo.collider.transform.gameObject;
                        if (SelectedGameObjectToDestroy != null)
                        {
                            var localization = GreenHellGame.Instance.GetLocalization();
                            SelectedItemToDestroy = SelectedGameObjectToDestroy?.GetComponent<Item>();
                            if (SelectedItemToDestroy != null && Item.Find(SelectedItemToDestroy.GetInfoID()) != null)
                            {
                                SelectedGameObjectToDestroyName =  localization.Get(SelectedItemToDestroy.GetInfoID().ToString()) ?? SelectedItemToDestroy?.GetName() ;
                            }
                            else
                            {
                                SelectedGameObjectToDestroyName =  localization.Get(SelectedGameObjectToDestroy?.name) ?? SelectedGameObjectToDestroy?.name;
                            }

                            ShowConfirmDestroyDialog(SelectedGameObjectToDestroyName);
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(DestroyOnHit));
            }
        }

        private void ShowConfirmDestroyDialog(string itemToDestroyName)
        {
            try
            {
                EnableCursor(true);
                string description = $"Are you sure you want to destroy { itemToDestroyName }?";
                YesNoDialog destroyYesNoDialog = GreenHellGame.GetYesNoDialog();
                destroyYesNoDialog.Show(this, DialogWindowType.YesNo, $"{ModName} Info", description, true, false);
                destroyYesNoDialog.gameObject.SetActive(true);
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(ShowConfirmDestroyDialog));
            }
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
                            LocalItemsManager.UnlockItemInNotepad(constructionItemInfo.m_ID);
                            LocalItemsManager.UnlockItemInfo(constructionItemInfo.m_ID.ToString());
                            ShowHUDInfoLog(constructionItemInfo.m_ID.ToString(), "HUD_InfoLog_NewEntry");
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
                HandleException(exc, nameof(UnlockAllConstructions));
            }
        }

        private void DestroySelectedItem()
        {
            try
            {
                if (SelectedItemToDestroy != null || SelectedGameObjectToDestroy != null)
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
                    ShowHUDBigInfo(HUDBigInfoMessage(ItemNotSelectedMessage(), MessageType.Warning, Color.yellow));
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(DestroySelectedItem));
            }
        }

        public void OnYesFromDialog()
        {
            DestroySelectedItem();
            EnableCursor(false);
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
