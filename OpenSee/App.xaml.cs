using Microsoft.UI;
using Microsoft.UI.Windowing;

namespace OpenSee;

public partial class App : Application
{
    int width = 1200;
    int height = 700;
	public App()
	{
		InitializeComponent();
        Microsoft.Maui.Handlers.WindowHandler.WindowMapper.AppendToMapping(nameof(IWindow), (handler, view) =>
        {
#if WINDOWS
            var nativeWindow = handler.NativeView;
            nativeWindow.Activate();
            IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow);
            WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            //TODO: this is hardcoded stuff for fullhd
            appWindow.MoveAndResize(new Windows.Graphics.RectInt32((1920 / 2) - width / 2, (1080 / 2) - height / 2, width, height));
#endif
        });
        MainPage = new MainPage();
	}
}
