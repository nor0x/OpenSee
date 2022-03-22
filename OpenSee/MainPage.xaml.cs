using Microsoft.Maui.Controls.Shapes;
using Microsoft.Playwright;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml.Controls;
using OpenSee.Common;
using OpenSee.Common.Helpers;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Path = System.IO.Path;

namespace OpenSee;

public partial class MainPage : ContentPage
{

    bool _entryAnimating;
    bool _imageAnimating;

    Random random;

    MainViewModel ViewModel => BindingContext as MainViewModel;

    public MainPage()
    {
        InitializeComponent();

#if WINDOWS
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("textbox", (h, v) =>
        {
            if (h.PlatformView is Microsoft.UI.Xaml.Controls.TextBox tb)
            {
                var placeholder = GetTemplateChild("PlaceholderTextContentPresenter") as TextBlock;
                if(placeholder is not null)
                {
                    placeholder.HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center;
                    placeholder.VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center;
                }

                tb.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
                tb.Resources["TextControlBorderBrushFocused"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White);
                tb.VerticalContentAlignment = Microsoft.UI.Xaml.VerticalAlignment.Bottom;
                tb.HorizontalContentAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center;
                tb.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White);
            }
        });

        Microsoft.Maui.Handlers.ActivityIndicatorHandler.Mapper.AppendToMapping("mymap", (h, v) =>
        {
            if (h.PlatformView is Microsoft.UI.Xaml.Controls.ProgressBar pb)
            {
                pb.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Blue);
                pb.HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center;
                pb.Width = 520;
            }
        });

        Microsoft.Maui.Handlers.ButtonHandler.Mapper.AppendToMapping("AllowFocusOnInteraction", (h, v) =>
        {
            if (h.PlatformView is Microsoft.UI.Xaml.Controls.Button button)
            {
                button.AllowFocusOnInteraction = false;
            }
        });
#endif

        WeakReferenceMessenger.Default.Register<OpenFolderMessage>(this, async (r, m) =>
        {
#if WINDOWS
            await Windows.System.Launcher.LaunchFolderPathAsync(m.Value);
#endif
        });

        WeakReferenceMessenger.Default.Register<UrlValidMessage>(this, async (r, m) =>
        {
#if WINDOWS
            AnimateEntry();
#endif
        });

        random = new Random();

    }


    protected async override void OnAppearing()
    {
        base.OnAppearing();
        await ViewModel.Init();
    }

    void StartAnimatingImage()
    {
        if (!_imageAnimating)
        {
            _imageAnimating = true;
            new Animation
            {
                { 0, 0.150, new Animation (v => CurrentImage.ScaleX = v, 1, 1.1) },
                { 0, 0.350, new Animation (v => CurrentImage.ScaleX = v, 1, 1.4) },
                { 0, 0.850, new Animation (v => CurrentImage.ScaleX = v, 1, 0) },
                { 0, 0.900, new Animation (v => CurrentImage.TranslationY = v, 0, 120) },
                { 0, 0.900, new Animation (v => CurrentImage.TranslationX = v, 0, 100) },
                { 0, 0.900, new Animation (v => CurrentImage.ScaleY = v, 1, 0) },
                { 0, 0.900, new Animation (v => CurrentImage.Opacity = v, 1, 0) },
            }
            .Commit(this, "DownloadImageAnimation", length: (uint)random.Next(100, 900), easing: Easing.CubicIn, finished: (d, b) =>
             {
                 Dispatcher.Dispatch(() =>
                 {
                     _imageAnimating = false;
                     CurrentImage.Source = ViewModel.AllUrls.GetRandomElement(); ;
                 });
             });
        }
    }


    void AnimateEntry()
    {
        if (!_entryAnimating)
        {
            _entryAnimating = true;

            new Animation {
                    { 0, 0.125, new Animation (v => UrlEntry.TranslationX = v, 0, -13) },
                    { 0.125, 0.250, new Animation (v => UrlEntry.TranslationX = v, -13, 13) },
                    { 0.250, 0.375, new Animation (v => UrlEntry.TranslationX = v, 13, -11) },
                    { 0.375, 0.5, new Animation (v => UrlEntry.TranslationX = v, -11, 11) },
                    { 0.5, 0.625, new Animation (v => UrlEntry.TranslationX = v, 11, -7) },
                    { 0.625, 0.75, new Animation (v => UrlEntry.TranslationX = v, -7, 7) },
                    { 0.75, 0.875, new Animation (v => UrlEntry.TranslationX = v, 7, -5) },
                    { 0.875, 1, new Animation (v => UrlEntry.TranslationX = v, -5, 0) }
                }
            .Commit(this, "AppleShakeChildAnimations", length: 500, easing: Easing.Linear, finished: (x, y) => _entryAnimating = false);
        }
    }

    private void SettingsButton_Clicked(object sender, EventArgs e)
    {
        if (!LoadingGrid.IsVisible)
        {
            SettingsGrid.IsVisible = !SettingsGrid.IsVisible;
        }
    }
}