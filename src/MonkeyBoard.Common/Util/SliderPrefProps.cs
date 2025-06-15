namespace MonkeyBoard.Common {
    public class SliderPrefProps {
        public int Min { get; set; }
        public int Max { get; set; }
        public int Default { get; set; }
        public SliderPrefType SliderType { get; set; }
        public SliderPrefProps(int def, int min, int max, SliderPrefType sliderType) {
            Default = def;
            Min = min;
            Max = max;
            SliderType = sliderType;
        }
        public override string ToString() {
            return base.ToString();
        }
    }
}
