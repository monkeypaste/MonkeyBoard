using MonkeyPaste.Common;
using MonkeyPaste.Common.Plugin;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBoard.Common {
    public class CulturePackInfo {
        public CultureInfo Info { get; set; }
        public bool IsInstalled { get; set; }
        public bool IsActive { get; set; }
    }
    public static class CultureManager {
        #region Private Variables
        #endregion

        #region Constants
        #endregion

        #region Statics
        #endregion

        #region Interfaces
        #endregion

        #region Properties

        #region Members
        static Dictionary<string, string> _specialRegionCultureLookup;
        static Dictionary<string, string> SpecialRegionCultureLookup {
            get {
                if(_specialRegionCultureLookup == null) {
                    _specialRegionCultureLookup = new() {
                        {"kc","Kachin" },
                        {"son","Songhay" },
                        {"tz","Tamazight" },
                    };
                }
                return _specialRegionCultureLookup;
            }
        }
        static List<KeyboardCollectionManifest> AvailablePacks { get; set; } = [];

        public static CulturePackInfo[] GetInstalledPackInfo() {
            var install_cultures = FindInstalledCultures();
            return install_cultures
                .Select(x => (x, CultureInfo.GetCultureInfo(x)))
                .Select(x => new CulturePackInfo() {
                    Info = x.Item2,
                    IsInstalled = true,
                    IsActive = x.x == CurrentKbCulture
                }).ToArray();
        }
        public static async Task<CulturePackInfo[]> GetAllPackInfoAsync() {
            if(!AvailablePacks.Any()) {
                AvailablePacks = await FetchKeyboardCollManifestsAsync();
            }
            var install_cultures = FindInstalledCultures();

            var cpil = new List<CulturePackInfo>();
            foreach(var ap in AvailablePacks) {
                // prevent en-US/en-GB and pt-BR/pt-PT dups
                var region_cultures =
                    ToCulturesFromRegion(ap.culture)
                    .Where(x => cpil.All(y => y.Info.Name != x.Name));

                cpil.AddRange(
                    region_cultures
                    .Select(x => new CulturePackInfo() {
                        Info = x,
                        IsInstalled = install_cultures.Contains(x.Name),
                        IsActive = x.Name == CurrentKbCulture
                    }));
            }

            return cpil.ToArray();
        }
        #endregion

        #region View Models
        public static IEnumerable<string> InstalledCultures =>
            FindInstalledCultures();
        #endregion

        #region Appearance
        #endregion

        #region Layout
        #endregion

        #region State

        #region Remote Index Uri
        public static string NEUTRAL_CC => "en-US";
        static bool IS_LOCAL =>
            false;
        static string LocalCultureIndexFileName => $"kb-index-local.json";
        static string RemoteCultureIndexFileName => $"kb-index.json";
        static string CultureIndexFileName => IS_LOCAL ? LocalCultureIndexFileName : RemoteCultureIndexFileName;
        static string LocalCultureIndexUri =>
            $"http://192.168.43.33:80/kb/{LocalCultureIndexFileName}";
        static string RemoteCultureIndexUri =>
            $"https://www.monkeypaste.com/dat/kb/{RemoteCultureIndexFileName}";

        static string CultureIndexUri =>
            IS_LOCAL ?
                LocalCultureIndexUri :
                RemoteCultureIndexUri;
        #endregion

        #region Resource Uris
        public static string AvCulturesBaseUri => $"{KbStorageHelpers.AvAssetsBaseUri}/Localization/packs";

        #endregion

        public static string CulturesRootDir => 
            Path.Combine(KbStorageHelpers.LocalStorageDir, "locale");

        public static string CurrentUiCultureDir =>
            Path.Combine(
                CulturesRootDir,
                CurrentUiCulture);
        public static string CurrentKbCultureDir =>
            Path.Combine(
                CulturesRootDir,
                CurrentKbCulture);
        static IKeyboardInputConnection InputConnection { get; set; }

        public static string CurrentUiCulture { get; private set; }
        public static string CurrentKbCulture { get; private set; }

        public static bool IsRtl { get; private set; } = false;
        public static bool IsBusy { get; private set; }
        public static bool IsLoaded { get; private set; }
        #endregion

        #region Models
        #endregion

        #endregion

        #region Events
        public static event EventHandler<int> DownloadProgressChanged;
        #endregion

        #region Constructors
        #endregion

        #region Public Methods
        public static void Init(IKeyboardInputConnection ic) {
            InputConnection = ic;
            if(InputConnection == null || InputConnection.SharedPrefService is not { } sps) {
                return;
            }

            string ui_cc = sps.GetPrefValue<string>(PrefKeys.DEFAULT_UI_CULTURE);
            if(string.IsNullOrEmpty(ui_cc)) {
                // initial startup
                ui_cc = CultureInfo.InstalledUICulture.Name;
                if(ui_cc == "en") {
                    // TODO need to link container to kb culture somehow
                    ui_cc = "en-US";
                }
                sps.SetPrefValue(PrefKeys.DEFAULT_UI_CULTURE, ui_cc);
            }

            string kb_cc = sps.GetPrefValue<string>(PrefKeys.DEFAULT_KB_CULTURE);
            if(string.IsNullOrEmpty(kb_cc)) {
                // initial startup
                kb_cc = ui_cc;
                sps.SetPrefValue(PrefKeys.DEFAULT_KB_CULTURE, kb_cc);
            }
            SetUiCulture(ui_cc);
            SetKbCulture(kb_cc);

            IsLoaded = true;
        }

        public static async Task GatherAvailableCulturesAsync() {
            AvailablePacks = await FetchKeyboardCollManifestsAsync();
        }

        public static async Task<bool> InstallCultureAsync(string cc) {
            bool wasBusy = IsBusy;
            try {
                IsBusy = true;
                if(!AvailablePacks.Any()) {
                    await GatherAvailableCulturesAsync();
                }
                var kbcl = AvailablePacks;
                if(kbcl == null) {
                    IsBusy = false;
                    return false;
                }

                if(kbcl.FirstOrDefault(x => x.culture == cc) is not { } kbc_manf) {
                    // no exact match, use locale
                    if(kbcl.FirstOrDefault(x => x.culture == cc.ToRegion()) is { } locale_kbc) {
                        kbc_manf = locale_kbc;
                    } else if(kbcl.FirstOrDefault(x => x.culture == NEUTRAL_CC) is { } neutral_kbc) {
                        // fallback to neutral
                        kbc_manf = neutral_kbc;
                    } else {
                        // error
                        IsBusy = false;
                        return false;
                    }
                }
                if(kbc_manf == null) {
                    IsBusy = false;
                    return false;
                }
                if(InstalledCultures.Contains(cc)) {
                    // already installed
                    IsBusy = false;
                    return true;
                }

                // download cul.zip and write bytes to temp.zip
                string cul_root_dir = CulturesRootDir;
                string temp_zip_path = Path.Combine(cul_root_dir, "temp.zip");

                await MpHttpRequester.DownloadAsync(
                    kbc_manf.collectionUri,
                    temp_zip_path,
                    (a, b, c) => { return UpdateProgress(a.Value, b, c.Value); });


                // extract temp.zip to <cul dir>
                string cul_dir = Path.Combine(cul_root_dir, cc);
                MpDebug.Assert(!cul_dir.IsDirectory(), $"Culture error, dir '{cc}' should not exist");
                if(cul_dir.IsDirectory()) {
                    MpFileIo.DeleteDirectory(cul_dir);
                }
                MpFileIo.CreateDirectory(cul_dir);
                ZipFile.ExtractToDirectory(temp_zip_path, cul_dir);

                // cleanup
                MpFileIo.DeleteFile(temp_zip_path);

                // rename region sub-extensions to match cc
                string region = cc.ToRegion();

                var file_map = new Dictionary<string, string>() {
                    {
                        Path.Combine(cul_dir, $"UiStrings.{region}.resx"),
                        Path.Combine(cul_dir, $"UiStrings.{cc}.resx")
                    },
                    {
                        Path.Combine(cul_dir, $"KeyboardStrings.{region}.resx"),
                        Path.Combine(cul_dir, $"KeyboardStrings.{cc}.resx")
                    },
                    {
                        Path.Combine(cul_dir, $"EmojiStrings.{region}.resx"),
                        Path.Combine(cul_dir, $"EmojiStrings.{cc}.resx")
                    },
                    {
                        Path.Combine(cul_dir, $"words_{region}.db"),
                        Path.Combine(cul_dir, $"words_{cc}.db")
                    }
                };
                foreach(var kvp in file_map) {
                    if(kvp.Value.IsFile()) {
                        // already ok
                        continue;
                    }
                    MpDebug.Assert(kvp.Key.IsFile(), $"Error excepted file does not exist: '{kvp.Key}'");
                    new FileInfo(kvp.Key).MoveTo(kvp.Value);
                }

                IsBusy = false;
                return true;

            }
            catch(Exception ex) {
                ex.Dump();
            }
            IsBusy = false;
            return false;
        }

        public static bool UninstallCulture(string cc) {
            if(!CanUninstallCulture(cc)) {
                MpDebug.Break($"Error cannot uninstall culture '{cc}'");
                return false;
            }
            // delete cul dir
            string cul_dir = GetCultureDir(cc);
            MpFileIo.DeleteDirectory(cul_dir);

            return true;
        }
        public static void SetUiCulture(string cc) {
            if(InputConnection.SharedPrefService is not { } sps) {
                return;
            }
            sps.SetPrefValue(PrefKeys.DEFAULT_UI_CULTURE, cc);
            CurrentUiCulture = cc;
            if(IsLoaded) {
                ResourceStrings.Init(cc, CurrentKbCulture);
            }
        }
        public static void SetKbCulture(string cc) {
            if(InputConnection.SharedPrefService is not { } sps) {
                return;
            }
            sps.SetPrefValue(PrefKeys.DEFAULT_KB_CULTURE, cc);
            CurrentKbCulture = cc;
            if(IsLoaded) {
                ResourceStrings.Init(CurrentUiCulture, cc);
            }
        }
        public static string GetCultureDir(string cc) {
            return Path.Combine(CulturesRootDir, cc.ToStringOrEmpty());
        }

        public static bool CanUninstallCulture(string cc) {
            return cc != NEUTRAL_CC;
        }

        #endregion

        #region Protected Methods
        #endregion

        #region Private Methods

        static IEnumerable<string> FindInstalledCultures() {
            // must be called from MainActivity on intial startup
            var dirs =
                Directory.GetDirectories(CulturesRootDir)
                .Select(x => Path.GetFileName(x));
            List<string> cultures = [];
            foreach(var dir_name in dirs) {
                try {
                    _ = new CultureInfo(dir_name);
                    cultures.Add(dir_name);
                }
                catch(CultureNotFoundException) {
                    continue;
                }
            }

            return cultures;
        }
        static async Task<List<KeyboardCollectionManifest>> FetchKeyboardCollManifestsAsync() {
            string index_json = await MpFileIo.ReadTextFromUriAsync(CultureIndexUri);
            var kbcl = index_json.DeserializeObject<List<KeyboardCollectionManifest>>();
            return kbcl ?? [];
        }
        static bool UpdateProgress(long totalBytes, long? bytesReceived, double percentComplete) {
            long TotalCount = totalBytes;
            long CurrentCount = bytesReceived.HasValue ? bytesReceived.Value : 0;
            int percent = totalBytes <= 0 ? 0 : Math.Min(100, (int)(((double)CurrentCount / (double)TotalCount) * 100));
            DownloadProgressChanged?.Invoke(typeof(CultureManager), percent);
            return false;
        }

        static string ToRegion(this string cc) {
            return cc.SplitNoEmpty("-").FirstOrDefault() ?? NEUTRAL_CC;
        }
        static IEnumerable<CultureInfo> ToCulturesFromRegion(this string region) {
            var parent = CultureInfo.GetCultureInfo(region.ToRegion());
            return CultureInfo.GetCultures(CultureTypes.AllCultures)
                                               .Where(x => x.Parent.Equals(parent));
        }
        public static string ToCultureDisplayName(this string cc) {
            if(CultureInfo.GetCultureInfo(cc) is not { } ci) {
                return string.Empty;
            }
            return ci.DisplayName;
        }
        #endregion

        #region Commands
        #endregion        
    }
}
