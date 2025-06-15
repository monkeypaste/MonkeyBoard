using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace MonkeyBoard.Android
{
    public class KeyboardLinearLayout : LinearLayout
    {
        int _height = 0;
        #region Ctors
        public KeyboardLinearLayout(Context context, int height) : base(context)
        {
            _height = height;
        }
        public KeyboardLinearLayout(Context context) : base(context)
        {
        }

        public KeyboardLinearLayout(Context context, IAttributeSet attrs) : base(context, attrs)
        {
        }

        public KeyboardLinearLayout(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
        {
        }

        public KeyboardLinearLayout(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs, defStyleAttr, defStyleRes)
        {
        }

        protected KeyboardLinearLayout(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }
        #endregion

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            base.OnMeasure(widthMeasureSpec, MeasureSpec.MakeMeasureSpec(_height, MeasureSpecMode.Exactly));
        }

    }
}
