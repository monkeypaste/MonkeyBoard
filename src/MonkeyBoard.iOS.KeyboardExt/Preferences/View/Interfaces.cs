namespace MonkeyBoard.iOS.KeyboardExt {

    public interface IRefreshData {
        void RefreshData();
    }
    public interface IKeyedElement {
        string Key { get; }
    }
    public interface ISummaryElement {
        string Summary { get; }
    }
}