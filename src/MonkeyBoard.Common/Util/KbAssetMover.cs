
using Avalonia.Platform;
using MonkeyPaste.Common;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using Stream = System.IO.Stream;

namespace MonkeyBoard.Common {
    public static class KbAssetMover {
        public static bool MoveAssets(bool force, string cc = "en-US") {
            // returns true if assets were moved ie. initial startup
            bool moved = false;
            moved = MoveCulture(force, cc);
            moved = MoveImages(force) || moved;
            return moved;
        }
        static bool MoveImages(bool force) {
            string[] imgs = [
                "about.png",
                "backspace.png",
                "checkround.png",
                "close.png",
                "delete.png",
                "dots_1x3.png",
                "dots_2x10.png",
                "edgearrowleft.png",
                "emoji.png",
                "enter.png",
                "error.png",
                "feedback.png",
                "globe.png",
                "kb_down_arrow.png",
                "lang.png",
                "open.png",
                "pref.png",
                "reset.png",
                "search.png",
                "shift.png",
                "shift_lock.png",
                "shift_on.png",
                ];
            string source_root_uri = KbStorageHelpers.AvAssetsBaseUri;
            string target_root_dir = KbStorageHelpers.ImgRootDir;
            if(target_root_dir.IsDirectory()) {
                if(force) {
                    MpFileIo.DeleteDirectory(target_root_dir);
                } else {
                    return false;
                }
            }
            MpFileIo.CreateDirectory(target_root_dir);

            foreach(string img in imgs) {
                if(!MoveAsset($"{source_root_uri}/Images/{img}", Path.Combine(target_root_dir, img))) {
                    return false;
                }
            }
            return true;
        }

        static bool MoveCulture(bool force, string cc) {
            try {
                string neutral_cul_dir = Path.Combine(CultureManager.CulturesRootDir, cc);
                if(neutral_cul_dir.IsDirectory()) {
                    if(force) {
                        MpFileIo.DeleteDirectory(neutral_cul_dir);
                    } else {
                        // already exists
                        return false;
                    }
                }
                string temp_zip_path = Path.Combine(KbStorageHelpers.LocalStorageDir, "temp.zip");
                string ca_source_uri = $"{CultureManager.AvCulturesBaseUri}/{cc}/{cc}.zip";
                if(!MoveAsset(ca_source_uri, temp_zip_path)) {
                    return false;
                }
                ZipFile.ExtractToDirectory(temp_zip_path, neutral_cul_dir);
                MpFileIo.DeleteFile(temp_zip_path);
                return true;
            }
            catch(Exception ex) {
                ex.Dump();
                return false;
            }
        }

        public static Stream LoadAvAssetStream(string asset_uri) {
            var asset_stream = AssetLoader.Open(new Uri(asset_uri));
            asset_stream.Seek(0, SeekOrigin.Begin);
            return asset_stream;
        }

        static bool MoveAsset(string source_uri, string target_path) {
            try {
                using(var asset_stream = LoadAvAssetStream(source_uri)) {
                    using(var ms = new MemoryStream()) {
                        asset_stream.CopyTo(ms);
                        ms.Seek(0, SeekOrigin.Begin);
                        File.WriteAllBytes(target_path, ms.ToArray());
                    }
                }
            }
            catch(Exception ex) {
                ex.Dump();
                return false;
            }
            return true;
        }
    }
}
