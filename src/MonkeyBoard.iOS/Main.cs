using UIKit;

namespace MonkeyBoard.iOS;

public class Application
{
    // This is the main entry point of the application.
    static void Main(string[] args)
    {
        // if you want to use a different Application Delegate class from "AppDelegate"
        // you can specify it here.
        //App.WaitForDebug(new object[] {App.WAIT_FOR_DEBUG_ARG});
        UIApplication.Main(args, null, typeof(AppDelegate));
    }
}
