using AppKit;
using Foundation;
using CommunityToolkit.Mvvm.Messaging;
using OpenSee.Common.Helpers;
using Xamarin.Forms;
using Xamarin.Forms.Platform.MacOS;

namespace OpenSeeXF.MacOS
{
    [Register("AppDelegate")]
    public class AppDelegate : FormsApplicationDelegate
    {
        NSWindow window;
        public AppDelegate()
        {
            var style = NSWindowStyle.Closable | NSWindowStyle.Resizable | NSWindowStyle.Titled;

            var rect = new CoreGraphics.CGRect(200, 1000, 1024, 768);
            window = new NSWindow(rect, style, NSBackingStore.Buffered, false);
            window.Title = "OpenSee👀"; // choose your own Title here
            window.TitleVisibility = NSWindowTitleVisibility.Hidden;

            WeakReferenceMessenger.Default.Register<OpenFolderMessage>(this, (r, m) =>
            {
                NSWorkspace.SharedWorkspace.OpenUrl(new NSUrl(m.Value, true));
            });
        }

        public override NSWindow MainWindow
        {
            get { return window; }
        }

        public override void DidFinishLaunching(NSNotification notification)
        {
            Forms.Init();
            LoadApplication(new App());
            base.DidFinishLaunching(notification);
        }
    }
}
