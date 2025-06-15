using AdvancedColorPicker;
using ARKit;
using CloudKit;
using CoreFoundation;
using CoreGraphics;
using Foundation;
using MonkeyPaste.Common;
using MonkeyPaste.Common.Avalonia;
using MonkeyPaste.Keyboard.Common;
using MonoTouch.Dialog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UIKit;
using WebKit;
using static SQLite.SQLite3;

#pragma warning disable CS1062 // The best overloaded Add method for the collection initializer element is obsolete
namespace MonkeyBoard.iOS.KeyboardExt {
    public class PrefView {
        #region Private Variables
        #endregion

        #region Constants

        const string INSTALLED_CUL_GROUP_NAME = "INSTALLED_CULTURES";
        const string AVAILABLE_CUL_GROUP_NAME = "AVAILABLE_CULTURES";
        const string ACTIVE_KB_GROUP_NAME = "ACTIVE_KB_GROUP_NAME";
        const string HAIR_STYLE_GROUP_NAME = "HAIR_STYLE_GROUP_NAME";
        const string SKIN_TONE_GROUP_NAME = "SKIN_TONE_GROUP_NAME";
        const string PREFS_ELM_NAME = "PREFS_ELM_NAME";

        #endregion

        #region Statics
        static PrefView _instance;
        public static PrefView Instance => _instance;


        #region Event Handlers

        public static void ListItemElement_OnSelected(object sender, EventArgs e) {
            if(sender is not ListItemElement list_item_elm) {
                return;
            }

            if(list_item_elm.Key.TryToEnum<PrefKeys>(out var pref_key) &&
                list_item_elm.TagObj.ToStringOrEmpty() is { } pref_val) {
                _instance.PrefService.SetPrefValue(pref_key, pref_val);
                MpConsole.WriteLine($"LI CHANGED {list_item_elm.Key} set to {pref_val}");
            } else {
                switch(list_item_elm.Key) {
                    case INSTALLED_CUL_GROUP_NAME:
                        CultureManager.SetKbCulture(list_item_elm.TagObj.ToStringOrEmpty());
                        MpConsole.WriteLine($"LI CHANGED {list_item_elm.TagObj} kb pack ins");
                        break;
                    case ACTIVE_KB_GROUP_NAME:
                        KeyboardLayoutFactory.SetDefaultKeyboard(list_item_elm.TagObj.ToStringOrEmpty());
                        MpConsole.WriteLine($"LI CHANGED {list_item_elm.TagObj} keyboard selected");
                        break;
                }
            }
            try {
                _instance.RefreshAllData();
            }
            catch(Exception ex) {
                ex.Dump();
                iosKeyboardViewController.SetError(ex.ToString());
            }
        }

        public static void Slider_ValueChanged(object sender, EventArgs e) {
            if(sender is not SliderElement slider_elm ||
                !Enum.TryParse(slider_elm.Key, out PrefKeys prefKey) ||
                !_instance.PrefService.DefPrefValLookup.TryGetValue(prefKey, out object defValObj) ||
                    defValObj is not SliderPrefProps spp) {
                iosFooterView.SetLabel($"Slider not found");
                return;
            }

            _instance.PrefService.SetPrefValue(prefKey, (int)slider_elm.Value);
            var test = _instance.PrefService.GetPrefValue<int>(prefKey);
            iosFooterView.SetLabel($"SLIDER CHANGED {prefKey} set to {test} Control: {(int)slider_elm.Value}");
            _instance.UpdateSliderDetail(slider_elm, spp);
        }

        public static void SwitchElement_ValueChanged(object sender, System.EventArgs e) {
            try {
                if(sender is not SwitchElement switch_elm) {
                    return;
                }
                PrefKeys pref_key = switch_elm.Key.ToEnum<PrefKeys>();
                _instance.PrefService.SetPrefValue(pref_key, switch_elm.Value);
                if(pref_key == PrefKeys.None) {
                    //MpConsole.WriteLine($"{switch_elm.Key} set to {switch_elm.Value}");
                } else {
                    MpConsole.WriteLine($"SWITCH CHANGED {pref_key} set to {switch_elm.Value}");
                }

                bool is_cmd_pref = false;
                bool needs_ui_reset = false;
                void FinishAction() {
                    if(needs_ui_reset) {
                        // TODO need to find kb elements here and update sel/labels
                        _instance.UpdateAll();
                    }

                    // restore cmd
                    if(is_cmd_pref) {
                        switch_elm.Value = false;
                    }
                }
                if(!switch_elm.Value) {
                    // presume after here these are cmd switches, ignore false
                    needs_ui_reset = true;
                    FinishAction();
                    return;
                }
                Action<Action> prefAction = null;
                _instance.PrefService.PrefClickActionLookup.TryGetValue(pref_key, out prefAction);

                switch(pref_key) {
                    case PrefKeys.DO_UNINSTALL_LANG: {
                            is_cmd_pref = true;

                            //needs_ui_reset = true;
                            _instance.Post(async () => {
                                bool confirm = await _instance.AlertYesNoAsync(
                                    _instance.DVC,
                                    ResourceStrings.U["ConfirmTitle"].value,
                                    ResourceStrings.U["ConfirmUninstallLabel"].value);

                                if(!confirm) {
                                    // canceled
                                    return;
                                }
                                string cc = switch_elm.TagObj.ToStringOrEmpty();
                                CultureManager.UninstallCulture(cc);

                                _instance.DVC.NavigationController.PopViewController(true);
                                _instance.MoveLangGroup(INSTALLED_CUL_GROUP_NAME, AVAILABLE_CUL_GROUP_NAME, cc);
                                FinishAction();

                            });
                            break;
                        }

                    case PrefKeys.DO_SET_UI_LANG: {
                            is_cmd_pref = true;
                            needs_ui_reset = true;
                            _instance.Post(async () => {
                                bool confirm = await _instance.AlertYesNoAsync(
                                    _instance.DVC,
                                    ResourceStrings.U["ConfirmTitle"].value,
                                    ResourceStrings.U["ConfirmSetLanguagelLabel"].value);

                                if(!confirm) {
                                    // canceled
                                    return;
                                }
                                CultureManager.SetUiCulture(switch_elm.TagObj.ToStringOrDefault());
                                _instance.Alert(
                                    _instance.DVC,
                                    ResourceStrings.U["SetLanguagelFollowUpTitle"].value,
                                    ResourceStrings.U["SetLanguagelFollowUpLabel"].value);

                                FinishAction();
                            });
                            break;
                        }

                    case PrefKeys.DO_RESTORE_DEFAULT_KB: {
                            is_cmd_pref = true;
                            needs_ui_reset = true;
                            KeyboardLayoutFactory.SetDefaultKeyboard(switch_elm.TagObj.ToStringOrEmpty());
                            FinishAction();
                            break;
                        }

                    case PrefKeys.DO_RESET_DB: {
                            is_cmd_pref = true;
                            prefAction?.Invoke(FinishAction);
                            //_instance.Post(async () => {
                            //    bool confirm = await _instance.AlertYesNoAsync(
                            //        _instance.DVC,
                            //        ResourceStrings.U["ConfirmResetCompDbTitle"].value,
                            //        ResourceStrings.U["ConfirmResetCompDbMsg"].value);
                            //    if (!confirm) {
                            //        // canceled
                            //        return;
                            //    }
                            //    await WordDb.ResetDbAsync_file();

                            //    _instance.Alert(_instance.DVC, msg: ResourceStrings.U["ResetComplDbCompleteMsg"].value);

                            //    FinishAction();
                            //});
                            break;
                        }

                    case PrefKeys.DO_RESET_ALL: {

                            is_cmd_pref = true;
                            needs_ui_reset = false;
                            prefAction?.Invoke(FinishAction);
                            //_instance.Post(async () => {
                            //    bool confirm = await _instance.AlertYesNoAsync(
                            //        //_instance.DVC,
                            //        iosKeyboardViewController.Instance,
                            //        ResourceStrings.U["ConfirmResetAllTitle"].value,
                            //        ResourceStrings.U["ConfirmResetAllMessage"].value);
                            //    if (confirm) {
                            //        try {
                            //            iosKeyboardViewController.Instance.Post(() => _instance.PrefService.RestoreDefaults());
                            //        }
                            //        catch (Exception ex) {
                            //            ex.Dump();
                            //            iosKeyboardViewController.SetError(ex.ToString());
                            //        }

                            //        await Task.Delay(500);
                            //        _instance.Alert(_instance.DVC, ResourceStrings.U["ResetCompleteText"].value);
                            //    }

                            //    FinishAction();

                            //});
                            break;
                        }

                    default:
                        switch(switch_elm.Key) {
                            case AVAILABLE_CUL_GROUP_NAME: {
                                    is_cmd_pref = true;
                                    //needs_ui_reset = true;
                                    MpConsole.WriteLine($"Cul to download: {switch_elm.TagObj.ToStringOrEmpty()}");
                                    _instance.Post(async () => {
                                        await _instance.DownloadAndInstallPackAsync(switch_elm);

                                        FinishAction();
                                    });

                                    break;
                                }

                        }
                        break;
                }
            }
            catch(Exception ex) {
                ex.Dump();
                iosKeyboardViewController.SetError(ex.ToString());
            }



        }


        #endregion

        #endregion

        #region Interfaces
        #endregion

        #region Properties

        #region Members
        public CustomDialogViewController ContainerViewController { get; private set; }
        public DialogViewController DVC { get; private set; }
        RootElement Root { get; set; }
        public SharedPrefWrapper PrefService { get; private set; }
        #endregion

        #region View Models
        #endregion

        #region Appearance
        public bool IsDark { get; private set; }
        #endregion

        #region Layout
        #endregion

        #region State
        #endregion

        #region Models
        #endregion

        #endregion

        #region Events
        //public event EventHandler OnClosed;
        #endregion

        #region Constructors
        #endregion

        #region Public Methods
        public async Task<DialogViewController> CreateDialogAsync(SharedPrefWrapper prefService, bool isDark) {
            _instance = this;
            PrefService = prefService;
            IsDark = isDark;
            try {
                Root = await CreateRootAsync();
                SetHiddenPrefs();
                DVC = new CustomDialogViewController(Root, true, isDark) {
                    Style = UITableViewStyle.Grouped
                };
                return DVC;
            }
            catch(Exception ex) {
                ex.Dump();
            }
            return null;
        }

        //private void DVC_ViewAppearing(object sender, EventArgs e) {
        //    //AttachHandlers();
        //    UpdateAll();
        //}

        //private void DVC_ViewDisappearing(object sender, EventArgs e) {
        //    //DetachHandlers();
        //}

        #endregion

        #region Protected Methods
        #endregion

        #region Private Methods
        void SetHiddenPrefs() {
            if(GetDescendants<Element>(Root) is not { } all_elms ||
                all_elms.OfType<RootElement>() is not { } root_elms ||
                all_elms.OfType<IKeyedElement>() is not { } keyed_elms) {
                return;
            }

            List<Element> to_remove = [];
            foreach(var keyed_elm in keyed_elms.ToList()) {
                var key = keyed_elm.Key.ToEnum<PrefKeys>();
                if(key == PrefKeys.None ||
                    !PrefService.IsHidden(key) ||
                    keyed_elm is not Element elm_to_remove ||
                    GetRoot(elm_to_remove) is not { } elm_root ||
                    GetSection(elm_to_remove) is not { } elm_sec
                    ) {
                    continue;
                }
                //to_remove.Add(elm_to_remove);
                elm_sec.Remove(elm_to_remove);
                if(elm_sec.Count == 0) {
                    elm_root.Remove(elm_sec);
                }
            }
        }
        void UpdateAll() {
            UpdateDependencies();
            UpdateSliderDetails();
            RefreshAllData();
        }
        void UpdateSliderDetails() {
            if(GetDescendants<SliderElement>(Root) is not { } slider_elms) {
                return;
            }
            foreach(var slider_elm in slider_elms) {
                if(!Enum.TryParse(slider_elm.Key, out PrefKeys prefKey) ||
                    !_instance.PrefService.DefPrefValLookup.TryGetValue(prefKey, out object defValObj) ||
                    defValObj is not SliderPrefProps spp) {
                    continue;
                }
                UpdateSliderDetail(slider_elm, spp);
            }
        }
        void UpdateSliderDetail(SliderElement slider_elm, SliderPrefProps spp) {
            int min = spp.Min;
            int max = spp.Max;
            int val = (int)slider_elm.Value;
            int disp_val = val;
            string suffix = string.Empty;
            switch(spp.SliderType) {
                case SliderPrefType.Percent:
                    suffix = "%";
                    disp_val = (int)(((double)val / (double)(max - min)) * 100d);
                    break;
                case SliderPrefType.Count:
                    break;
                case SliderPrefType.Milliseconds:
                    suffix = "ms";
                    break;
            }
            string detail = $"{disp_val} {suffix}";
            slider_elm.SetContent(slider_elm.Caption, detail);
        }

        void UpdateDependencies() {
            if(GetDescendants<Element>(Root) is not { } all_elms) {
                return;
            }
            foreach(var dep_kvp in PrefService.DepLookup) {
                if(all_elms.OfType<SwitchElement>().FirstOrDefault(x => x.Key == dep_kvp.Key.ToString()) is not { } key_elm) {
                    //MpConsole.WriteLine($"Key not found {dep_kvp.Key}");
                    continue;
                }
                if(all_elms.OfType<IKeyedElement>().Where(x => dep_kvp.Value.Any(y => y.ToString() == x.Key)) is not { } dep_elms) {
                    continue;
                }
                foreach(var dep_elm in dep_elms.OfType<Element>()) {
                    if(dep_elm.GetContainerTableView() is not { } tv ||
                        dep_elm.GetCell(tv) is not { } dep_cell) {
                        continue;
                    }
                    dep_cell.SelectionStyle = key_elm.Value ? UITableViewCellSelectionStyle.Default : UITableViewCellSelectionStyle.Gray;
                    dep_cell.UserInteractionEnabled = key_elm.Value;
                    if(dep_cell.AccessoryView is UIControl uic) {
                        uic.Enabled = key_elm.Value;
                    }
                }
            }
        }

        void RefreshAllData() {
            DVC.ReloadData();
            if(ThemedRootElement.KeyedRoots.FirstOrDefault(x => x.Key == PREFS_ELM_NAME) is { } pref_elm &&
                pref_elm.Count > 1 &&
                pref_elm[1] is { } sec) {
                pref_elm.Reload(sec, UITableViewRowAnimation.Automatic);
            }
        }

        #endregion

        #region Elements

        async Task<RootElement> CreateRootAsync() {
            var lang_elm = await CreateLangsAsync();

            return new ThemedRootElement(ResourceStrings.U["MainTitle"].value) {
                new Section() {
                    CreatePrefs(),
                    lang_elm,
                },
                new Section() {
                    CreateFeedback(),
                    CreateAbout()
                },
                new Section() {
                    new SwitchElement(ResourceStrings.U["ResetAllTitle"].value,false,PrefKeys.DO_RESET_ALL.ToString())
                }
            };
        }
        Element CreatePref(PrefKeys prefKey) {
            if(!ResourceStrings.U.TryGetValue($"{prefKey}_TITLE", out var titleObj) ||
                titleObj.value is not { } title) {
                title = string.Empty;
            }
            if(!ResourceStrings.U.TryGetValue($"{prefKey}_DETAIL", out var detailObj) ||
                detailObj.value is not { } detail) {
                detail = string.Empty;
            }
            Element pref = null;
            if(!PrefService.DefPrefValLookup.TryGetValue(prefKey, out object prefValObj)) {
                return null;
            } else {
                switch(prefValObj) {
                    case bool boolVal:
                        var bool_pref = new SwitchElement(
                            title, detail,
                            PrefService.GetPrefValue<bool>(prefKey),
                            prefKey.ToString());
                        pref = bool_pref;
                        break;
                    case SliderPrefProps spp:
                        var int_pref = new SliderElement(
                            PrefService.GetPrefValue<int>(prefKey),
                            spp.Min,
                            spp.Max,
                            prefKey.ToString(),
                            title);
                        pref = int_pref;
                        break;
                    case string strVal:
                        switch(prefKey) {
                            case PrefKeys.CUSTOM_BG_COLOR: {
                                    //var color_pref = new ColorPreference(context, null);
                                    //if (PrefService.GetPrefValue<string>(PrefKeys.CUSTOM_BG_COLOR) is not { } color_hex) {
                                    //    color_hex = MpColorHelpers.GetRandomHexColor();
                                    //}
                                    //color_pref.SetPresets(KeyboardPalette.PalettePresets.Select(x => x.ToAdColor()).ToArray());
                                    //color_pref.SetDefaultValue(Integer.ParseInt(color_hex.ToAdColor().ToInt().ToString()));
                                    //color_pref.PreferenceChange += Color_pref_PreferenceChange1;
                                    //pref = color_pref;
                                }

                                break;
                            case PrefKeys.CUSTOM_BG_PATH: {
                                    //if (PrefService.GetPrefValue<string>(prefKey) is { } prefPath &&
                                    //    !string.IsNullOrEmpty(prefPath)) {
                                    //    detail = Path.GetFileName(prefPath);
                                    //} else {
                                    //    // none selected
                                    //    detail = ResourceStrings.U["CommonNoneText"].value;
                                    //}

                                    //var file_pref = new Preference(context);
                                    //file_pref.PreferenceClick += File_pref_PreferenceClick;
                                    //pref = file_pref;
                                }
                                break;
                            default: {
                                    var itemsPrefix = $"{prefKey}_ITEM";
                                    var itemsCount = ResourceStrings.U.Keys.Where(x => x.StartsWith(itemsPrefix)).Count();
                                    var subEnum = PrefService.GetListEnum(prefKey);
                                    var entries = Enumerable.Range(0, itemsCount)
                                            .Select(x => ResourceStrings.U[$"{itemsPrefix}{x}"].value)
                                            .ToArray();
                                    var enumType = PrefService.GetListEnum(prefKey);
                                    var sel_idx = (int)PrefService.GetPrefValue<string>(prefKey).ToEnum(enumType);
                                    var list_pref = new ThemedRootElement(
                                        prefKey.ToString(),
                                        title,
                                        new ListItemGroup(prefKey.ToString(), sel_idx)) {
                                                        new Section(){
                                                    Enumerable.Range(0,itemsCount)
                                                    .Select(x=>ResourceStrings.U[$"{itemsPrefix}{x}"].value)
                                                    .Select((x,idx)=>new ListItemElement(x,prefKey.ToString()) {
                                                        TagObj = Enum.GetNames(enumType)[idx].ToEnum(enumType)
                                                    } )
                                                }
                                            };
                                    if(string.IsNullOrEmpty(detail) && entries.ElementAtOrDefault(sel_idx) is { } sel_label) {
                                        detail = sel_label;
                                    }
                                    pref = list_pref;
                                }
                                break;
                        }
                        break;

                    default:
                        // unhandled
                        Debugger.Break();
                        break;
                }
            }
            return pref;
        }
        ThemedRootElement CreateCategory(PrefCategoryType cat) {
            if(!ResourceStrings.U.TryGetValue($"{cat}_TITLE", out var catObj) ||
                    catObj.value is not { } cat_title) {
                cat_title = string.Empty;
            }

            if(PrefService.CatLookup.TryGetValue(cat, out var pref_keys) &&
                pref_keys.Select(CreatePref).Where(x => x != null) is { } pref_elms &&
                pref_elms.Any()) {
                return new ThemedRootElement(cat_title) {
                    new Section() {
                        pref_elms
                    }
                };
            }
            return new(cat_title);
        }
        RootElement CreatePrefs() {
            return new ThemedRootElement(PREFS_ELM_NAME, ResourceStrings.U["PREFERENCES_TITLE"].value) {
                new Section() {
                    Enum.GetNames(typeof(PrefCategoryType))
                .Select(x=>x.ToEnum<PrefCategoryType>())
                .Where(x=>x != PrefCategoryType.None)
                .Select(x=>CreateCategory(x))
                }

            };
        }
        void ShowColorPicker() {
            iosFooterView.SetLabel($"Showing color picker...");
            var picker = new ColorPickerViewController();
            picker.ColorPicked += () => {
                string hex = picker.SelectedColor.ToHex();
                PrefService.SetPrefValue<string>(PrefKeys.CUSTOM_BG_COLOR, hex);
            };
            picker.Title = "Pick a color!";
            //DVC.ViewAppearing += (s, e) => {
            //    iosFooterView.SetLabel($"View appearing: picker view is null: {picker.View == null}");
            //    if(picker.View != null) {

            //        picker.View.Frame = DVC.View.Frame;
            //        picker.View.Redraw(true);
            //    }
            //};
            picker.ModalInPresentation = true;
            //picker.ModalInPopover = true;
            //var pickerNav = new UINavigationController(picker);
            //pickerNav.ModalPresentationStyle = UIModalPresentationStyle.FormSheet;

            //var doneBtn = new UIBarButtonItem(UIBarButtonSystemItem.Done);
            //picker.NavigationItem.RightBarButtonItem = doneBtn;
            //doneBtn.Clicked += (s, e) => {
            //};
            DVC.PresentViewController(picker, true, null);
        }

        //async Task ShowFilePickerAsync() {
        //    iosFooterView.SetLabel($"Showing file picker...");
        //    async Task<FileResult> PickAndShow(PickOptions options) {
        //        try {
        //            var result = await FilePicker.Default.PickAsync(options);
        //            return result;
        //        }
        //        catch(Exception ex) {
        //            // The user canceled or something went wrong
        //            ex.Dump();
        //        }
        //        return null;
        //    }

        //    var result = await PickAndShow(new PickOptions() { FileTypes = FilePickerFileType.Images });
        //    if(result is not { } fr) {
        //        return;
        //    }

        //FilePickerFileType filePickerFileType = new FilePickerFileType(
        //new Dictionary<DevicePlatform, IEnumerable<string>> {
        //            { DevicePlatform.iOS, new [] { "*.png", "*.jpg", "*.jpeg", "*.gif", "*.bmp", "*.webp" } },
        //});

        //PickOptions pickOptions = new PickOptions {
        //    PickerTitle = "Select all the files",
        //    FileTypes = filePickerFileType,
        //};
        //var results = await FilePicker.PickMultipleAsync(pickOptions);
        //if (results == null) {
        //    lblFileContents.Text = "Return NULL";
        //    return;
        //}
        //lblFileContents.Text = $"  {results.Count()} files picked\r\n";
        //foreach (var res in results) {
        //    try {
        //        var stream = await res.OpenReadAsync();
        //        lblFileContents.Text += $"File Name: {res.FileName}, len={stream.Length}, path={res.FullPath}:\r\n";
        //    }
        //    catch (Exception ex) {
        //        lblFileContents.Text += $"Exception:  {ex}";
        //    }
        //}
        //PrefService.SetPrefValue(PrefKeys.CUSTOM_BG_PATH, fr.FullPath);
        //}

        #region Langs
        async Task DownloadAndInstallPackAsync(SwitchElement switch_elm) {
            void OnProgressChanged(object sender, int percent) {
                switch_elm.SetProgress(percent);
            }
            string cc = switch_elm.TagObj.ToStringOrEmpty();
            CultureManager.DownloadProgressChanged += OnProgressChanged;
            bool success = await CultureManager.InstallCultureAsync(cc);
            CultureManager.DownloadProgressChanged -= OnProgressChanged;
            if(success) {
                Alert(DVC, "Success", "DONE!");
                _instance.MoveLangGroup(AVAILABLE_CUL_GROUP_NAME, INSTALLED_CUL_GROUP_NAME, cc);
            } else {
                //MpConsole.WriteLine($"Install failed");
                Alert(DVC, ResourceStrings.U["ErrorTitle"].value, ResourceStrings.U["ErrorLabel"].value);
            }
        }

        IEnumerable<Element> GetCommandElements(string cc, KeyboardCollectionFormat kbc, string sel_cc) {
            List<Element> cmd_elms = [];
            if(sel_cc != null && cc == sel_cc) {
                if(kbc.keyboards.FirstOrDefault(x => x.isDefault).guid != KeyboardLayoutFactory.DefaultKeyboardGuid) {
                    cmd_elms.Add(new SwitchElement(
                        ResourceStrings.U["RestoreDefaultKbLabel"].value,
                        PrefService.GetPrefValue<bool>(PrefKeys.DO_RESTORE_DEFAULT_KB),
                        PrefKeys.DO_RESTORE_DEFAULT_KB.ToString()) {
                        TagObj = kbc.keyboards.FirstOrDefault(x => x.isDefault).guid
                    });
                }
            }
            if(cc != CultureManager.CurrentUiCulture) {
                cmd_elms.Add(new SwitchElement(
                    ResourceStrings.U["SetLanguagelLabel"].value,
                    ResourceStrings.U["SetLanguagelDetail"].value,
                    PrefService.GetPrefValue<bool>(PrefKeys.DO_SET_UI_LANG),
                    PrefKeys.DO_SET_UI_LANG.ToString()) {
                    TagObj = cc
                });
            }
            if(CultureManager.CanUninstallCulture(cc)) {
                cmd_elms.Add(new SwitchElement(
                        ResourceStrings.U["UninstallLabel"].value,
                        PrefService.GetPrefValue<bool>(PrefKeys.DO_UNINSTALL_LANG),
                        PrefKeys.DO_UNINSTALL_LANG.ToString()) {
                    TagObj = cc
                });
            }
            return cmd_elms;
        }
        void MoveLangGroup(string fromGroup, string toGroup, string cc) {
            if(_instance.GetDescendants<SwitchElement>(_instance.Root) is not { } switch_elms ||
                _instance.GetDescendants<ListItemElement>(_instance.Root) is not { } li_elms ||
                            switch_elms.Where(x => x.Key == AVAILABLE_CUL_GROUP_NAME) is not { } avail_switch_elms ||
                            li_elms.Where(x => x.Key == AVAILABLE_CUL_GROUP_NAME) is not { } install_li_elms ||
                            install_li_elms.FirstOrDefault() is not { } first_install_li_elm ||
                            first_install_li_elm.Parent.Parent is not RootElement root_install_elm) {
                return;
            }
            if(fromGroup == AVAILABLE_CUL_GROUP_NAME) {
                if(avail_switch_elms.FirstOrDefault(x => x.TagObj.ToStringOrEmpty() == cc) is { } to_move_switch) {
                    if(to_move_switch.Parent is RootElement re) {
                        if(re[1] is { } avail_sec) {
                            avail_sec.Remove(to_move_switch);
                            var kbc = KeyboardLayoutFactory.LoadKeyboardCollection(cc);
                            var new_install_elm = CreateInstallLangElement(cc, CultureManager.CurrentKbCulture, kbc, -1);
                            root_install_elm[0].Add(new_install_elm);
                            MpConsole.WriteLine($"Move success");
                        }
                    } else {
                        MpConsole.WriteLine($"FAIL to_move_switch.Parent is {to_move_switch.Parent.GetType()}");
                    }
                } else {
                    MpConsole.WriteLine($"FAIL {cc} not found in {avail_switch_elms.Count()} switch elms");

                }
            } else {
                if(install_li_elms.FirstOrDefault(x => x.TagObj.ToStringOrEmpty() == cc) is { } to_move_li) {
                    if(to_move_li.Parent is RootElement li_root_elm) {
                        li_root_elm[0].Remove(to_move_li);
                        if(li_root_elm.Parent is RootElement re) {
                            var new_avail_elm = CreateAvailLangElement(cc);
                            re[1].Add(new_avail_elm);
                            MpConsole.WriteLine($"Move success");
                        } else {
                            MpConsole.WriteLine($"FAIL li_root_elm.Parent is {li_root_elm.Parent.GetType()}");

                        }
                    } else {
                        MpConsole.WriteLine($"FAIL to_move_li.Parent is {to_move_li.Parent.GetType()}");

                    }
                } else {
                    MpConsole.WriteLine($"FAIL {cc} not found in {install_li_elms.Count()} li elms");
                }
            }
        }
        string GetCultureLabel(string cc) {
            string full_name = NSLocale.CurrentLocale.GetIdentifierDisplayName(cc);

            if(full_name.SplitNoEmpty("(") is { } name_parts &&
                name_parts.Length > 1 &&
                name_parts[1] is { } country_part &&
                country_part.Replace(")", string.Empty) is { } country_name &&
                ResourceStrings.E.FirstOrDefault(x => x.Value.value.ToLower().Contains(country_name.ToLower())) is { } flag_kvp) {
                full_name = flag_kvp.Key + " " + full_name;
            }
            return full_name;
        }
        RootElement CreateInstallLangElement(string cc, string sel_cc, KeyboardCollectionFormat kbc, int def_kb_idx) {
            return new ThemedRootElement(INSTALLED_CUL_GROUP_NAME,
                            GetCultureLabel(cc),
                            new ListItemGroup(INSTALLED_CUL_GROUP_NAME, sel_cc != null && cc == sel_cc ? def_kb_idx : -1)) {
                                    new Section (){
                                            KeyboardLayoutFactory.LoadKeyboardCollection(cc).keyboards.Select(
                                                y=>new ListItemElement(y.label,y.description,ACTIVE_KB_GROUP_NAME) {
                                                    TagObj = y.guid
                                                })
                                    },
                                    new Section() {
                                        GetCommandElements(cc, kbc, sel_cc)
                                    }
                        };
        }
        SwitchElement CreateAvailLangElement(string cc) {
            return new SwitchElement(false, GetCultureLabel(cc), false, AVAILABLE_CUL_GROUP_NAME) {
                TagObj = cc
            };
        }
        async Task<RootElement> CreateLangsAsync() {
            var installed_items = CultureManager.GetInstalledPackInfo().OrderBy(x => x.Info.DisplayName);
            CulturePackInfo sel_cc = installed_items.FirstOrDefault(x => x.IsActive);
            int sel_install_idx = installed_items.IndexOf(sel_cc);
            var kbc = KeyboardLayoutFactory.LoadKeyboardCollection(sel_cc.Info.Name);
            int def_kb_idx = kbc.keyboards.IndexOf(kbc.keyboards.FirstOrDefault(x => x.guid == KeyboardLayoutFactory.DefaultKeyboardGuid));

            var all_avail_items = await CultureManager.GetAllPackInfoAsync();
            var avail_items =
                all_avail_items
                .Where(x => x.Info != null && installed_items.All(y => y.Info.Name != x.Info.Name))
                .OrderBy(x => x.Info.DisplayName);

            //MpConsole.WriteLine($"Total kb packs found: {all_avail_items.Length}", true);
            MpConsole.WriteLine($"Total kb packs found: {all_avail_items.Length}", true);

            Section avail_sec = null;

            if(avail_items.Any()) {
                avail_sec = new Section(ResourceStrings.U["AvailableLanguagesTitle"].value){
                    avail_items.Select(x=>CreateAvailLangElement(x.Info.Name))
                };
            } else {
                // conn error
                avail_sec = new Section(ResourceStrings.U["ErrorTitle"].value){
                    new StringElement(ResourceStrings.U["ErrorLabel"].value)
                };
            }

            return new ThemedRootElement(ResourceStrings.U["LANG_PACKS_TITLE"].value) {
                new Section (ResourceStrings.U["InstalledLanguagesTitle"].value){
                        installed_items.Select(x=>CreateInstallLangElement(x.Info.Name,sel_cc.Info.Name,kbc,def_kb_idx))
                },
                avail_sec
            };

        }

        #endregion
        RootElement CreateFeedback() {
            return new ThemedRootElement(ResourceStrings.U["FEEDBACK_TITLE"].value) {
            };
        }
        RootElement CreateAbout() {
            return new ThemedRootElement(ResourceStrings.U["ABOUT_TITLE"].value) {
            };
        }

        #endregion

        #region Alerts


        void Alert(UIViewController anchor, string title = "", string msg = "") =>
            iosHelpers.Alert(anchor, title, msg, IsDark);

        Task<bool> AlertYesNoAsync(UIViewController vc, string title = "", string msg = "") =>
            iosHelpers.AlertYesNoAsync(vc, title, msg, IsDark);
        #endregion

        #region Helpers 
        Section GetSection(Element elm) {
            if(GetRoot(elm) is not { } re) {
                return null;
            }
            foreach(var sec in re) {
                for(int i = 0; i < sec.Count; i++) {
                    var sec_elm = sec[i];
                    if(sec_elm == elm) {
                        return sec;
                    }
                }
            }
            return null;
        }
        RootElement GetRoot(Element elm, bool includeSelf = false) {
            if(elm == null) {
                return null;
            }
            if(elm is RootElement re && includeSelf) {
                return re;
            }
            return GetRoot(elm.Parent, true);
        }
        IEnumerable<T> GetDescendants<T>(Element elm, bool includeSelf = false) where T : Element {
            List<T> descendants = [];
            if(elm is T root_t && includeSelf) {
                descendants.Add(root_t);
            }
            if(elm is not IEnumerable elm_enumerable) {
                return descendants;
            }

            foreach(var child in elm_enumerable) {
                if(child is Element child_elm) {
                    descendants.AddRange(GetDescendants<T>(child_elm, true));
                }
            }
            return descendants;
        }
        IEnumerable<RadioGroup> GetGroups(Element elm, bool includeSelf = false) {
            if(GetDescendants<RootElement>(elm, true) is not { } root_elms ||
                root_elms.Select(x => x.GetPrivateFieldValue<Group>("group")) is not { } groups) {
                return [];
            }
            return groups.OfType<RadioGroup>();
        }
        void Post(Action action) {
            DispatchQueue.MainQueue.DispatchAsync(() => {
                try {
                    action.Invoke();
                }
                catch(Exception ex) {
                    ex.Dump();
                }
            });
        }
        #endregion
    }
}