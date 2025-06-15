using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using Size = Avalonia.Size;

namespace iosKeyboardTest {

    public static class RenderHelpers {
        public static Bitmap RenderToBitmap(byte[] bytes) {
            using(var ms = new MemoryStream(bytes)) {
                var bmp = new Bitmap(ms);
                return bmp;
            }
        }
        public static Bitmap RenderToBitmap(Control target, double scale = 1, int quality = 100) {
            int pw = (int)(target.Width * scale);
            int ph = (int)(target.Height * scale);
            var pixelSize = new PixelSize(pw,ph);

            double h = target.Height;
            double w = target.Width;
            var size = new Size(w, h);

            double dpi_scale = scale;
            var dpi = new Vector(96 * dpi_scale, 96 * dpi_scale);
            using (RenderTargetBitmap rtbmp = new RenderTargetBitmap(pixelSize, dpi)) {
                target.Measure(size);
                target.Arrange(new Rect(size));
                rtbmp.Render(target);
                using (var outStream = new MemoryStream()) {
                    rtbmp.Save(outStream, quality);
                    outStream.Seek(0, SeekOrigin.Begin);
                    var outBmp = new Bitmap(outStream);
                    return outBmp;
                }
            }
        }
        public static byte[] RenderToByteArray(Control target, double scale = 1, int quality = 100) {
            if(RenderToBitmap(target,scale,quality) is not { } rtbmp) {
                return null;
            }
            using (var stream = new MemoryStream()) {
                rtbmp.Save(stream, quality);
                var result = stream.ToArray();
                rtbmp.Dispose();
                return result;
            }

        }
        public static void RenderToFile(Control target, string path) {
            if(RenderToBitmap(target) is not { } bmp) {
                return;
            }
            bmp.Save(path);
            bmp.Dispose();
        }
    }
}
