using Android.Content;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using AndroidX.Preference;
using System.Linq;

namespace MonkeyBoard.Android {
    public class CustomPref : Preference {
        public CustomPref(Context context) : base(context) { }
    }
    public class CustomPrefCategory : PreferenceCategory {
        public CustomPrefCategory(Context context) : base(context) { }
        public override void OnBindViewHolder(PreferenceViewHolder holder) {
            base.OnBindViewHolder(holder);
            var test = holder.ItemView.Descendants(true).Select(x => x.GetType().ToString()).ToList();
            if(holder.ItemView.Descendants(true).OfType<TextView>().Where(x=>x.Parent is ViewGroup).FirstOrDefault() is { } tv &&
                tv.Parent is ViewGroup tv_vg) {
                tv_vg.SetClipChildren(false);
                tv.ClipToOutline = false;
                tv.TextSize = 36;
                holder.ItemView.SetBackgroundColor(Color.Gray);
                tv_vg.TranslationX -= 20;
            }
            return;
        }
    }
}
