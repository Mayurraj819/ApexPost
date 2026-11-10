using Android.App;
using Android.Runtime;

namespace ApexPost.Courier;
[Application]
public class MainApplication(nint handle, JniHandleOwnership ownership) : MauiApplication(handle, ownership)
{
    protected override MauiApp CreateMauiApp()
    {
        Couchbase.Lite.Support.Droid.Activate(this);
        return MauiProgram.CreateMauiApp();
    }
}
