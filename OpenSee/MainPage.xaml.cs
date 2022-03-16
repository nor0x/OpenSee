using Microsoft.Maui.Controls.Shapes;
using Microsoft.Playwright;
using OpenSee.Helpers;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Path = System.IO.Path;

namespace OpenSee;

public partial class MainPage : ContentPage
{
    IPlaywright _playwright;
    IPage page;
    bool _entryAnimating;
    bool _imageAnimating;
    bool _isDownloading;
    bool _downloadRequested;
    bool _browserReady;

    int totalCount;

    string downloadFolder;
    string collectionFolder;
    ObservableCollection<string> allUrls = new ObservableCollection<string>();
    List<string> downloadUrls = new List<string>();

    string downloadSuffix = "=w500";
    Random random;

    public MainPage()
    {
        InitializeComponent();

#if WINDOWS
        Microsoft.Maui.Handlers.EntryHandler.EntryMapper.AppendToMapping("microsoft-ui-xaml/issues/5386", (h, v) =>
        {
            if (h.NativeView is Microsoft.UI.Xaml.Controls.TextBox tb)
            {
                tb.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
                tb.Resources["TextControlBorderBrushFocused"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White);
                tb.VerticalContentAlignment = Microsoft.UI.Xaml.VerticalAlignment.Bottom;
                tb.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White);
            }
        });

        Microsoft.Maui.Handlers.ActivityIndicatorHandler.Mapper.AppendToMapping("mymap", (h, v) =>
        {
            if (h.NativeView is Microsoft.UI.Xaml.Controls.ProgressBar pb)
            {
                pb.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Blue);
                pb.HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center;
                pb.Width = 520;
            }
        });

        Microsoft.Maui.Handlers.ButtonHandler.Mapper.AppendToMapping("AllowFocusOnInteraction", (h, v) =>
        {
            if (h.NativeView is Microsoft.UI.Xaml.Controls.Button button)
            {
                button.AllowFocusOnInteraction = false;
            }
        });
#endif

        downloadFolder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        random = new Random();
        CurrentImage.PropertyChanged += CurrentImage_PropertyChanged;
    }

    private async void CurrentImage_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CurrentImage.IsLoading))
        {
            if (!CurrentImage.IsLoading)
            {
                StartAnimatingImage();
            }
        }
    }


    protected async override void OnAppearing()
    {
        base.OnAppearing();

        _playwright = await Playwright.CreateAsync();
        _browserReady = File.Exists(_playwright.Firefox.ExecutablePath);
        if (!_browserReady)
        {
            StatusLabel.Text = "warming up...";
            await Task.Run(() =>
            {
                Microsoft.Playwright.Program.Main(new string[] { "install", "firefox" });
                _browserReady = true;
                if (_downloadRequested)
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        DownloadButton_Clicked(null, EventArgs.Empty);
                    });
                }
            });
        }
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
                 Device.BeginInvokeOnMainThread(() =>
                 {
                     _imageAnimating = false;
                     CurrentImage.Source = allUrls.GetRandomElement(); ;
                 });
             });
        }
    }

    async Task Reset()
    {
        _isDownloading = false;
        totalCount = 0;
        allUrls.Clear();
        downloadUrls.Clear();
        await page?.CloseAsync();
        DownloadButton.FontFamily = "Lato";
        DownloadButton.Text = "Download";
        DownloadProgress.Progress = 0;
        StatusLabel.Text = "";
        LoadingGrid.IsVisible = false;
        SettingsGrid.IsVisible = false;
    }


    async Task CheckImageSources(IReadOnlyList<IElementHandle> handles)
    {
        ParallelOptions parallelOptions = new()
        {
            MaxDegreeOfParallelism = 5
        };

        using HttpClient client = new();
        await Parallel.ForEachAsync(handles, parallelOptions, async (handle, token) =>
        {
            var sourceHandle = await handle.GetAttributeAsync("src");
            var source = sourceHandle.ToString();
            if (!source.Contains(".svg"))
            {
                if (!allUrls.Contains(source))
                {
                    allUrls.Add(source);
                    var blankSource = source.Substring(0, source.IndexOf("=w"));

                    var bytes = await client.GetByteArrayAsync(blankSource + downloadSuffix);
                    await File.WriteAllBytesAsync(Path.Combine(collectionFolder, Guid.NewGuid().ToString("N") + ".png"), bytes);

                    downloadUrls.Add(source);
                    var progress = (double)(downloadUrls.Count()) / totalCount;
                    await DownloadProgress.ProgressTo(progress, 100, Easing.CubicOut);
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        StatusLabel.Text = Math.Round(progress * 100) + "%";
                    });
                }
            }
        });
    }

    async Task CollectUrls()
    {
        StartAnimatingImage();
        while (allUrls.Count() != totalCount)
        {
            var collectionHandle = await page.QuerySelectorAsync(".AssetSearchView--results");
            var imgs = await collectionHandle.QuerySelectorAllAsync("img");
            await CheckImageSources(imgs);
            await page.EvaluateAsync("window.scrollBy(0,document.body.scrollHeight)");
        }
        await Reset();
    }

    private async void DownloadButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (_isDownloading)
            {
                await Reset();
            }
            else
            {
                DownloadButton.FontFamily = "TablerIcons";
                DownloadButton.Text = IconFont.x;
                LoadingGrid.IsVisible = true;
                SettingsGrid.IsVisible = false;
                if (!_browserReady)
                {
                    _downloadRequested = true;
                    return;
                }
                StatusLabel.Text = "looking for collection...";

                _isDownloading = true;
                var url = UrlEntry.Text;
                Uri uriResult;
                if (Uri.TryCreate(url, UriKind.Absolute, out uriResult) || uriResult.Host.ToLower().Contains("opensea.io") == false)
                {
                    await using var browser = await _playwright.Firefox.LaunchAsync(new() { Headless = true });
                    page = await browser.NewPageAsync();
                    var collectionUrl = $"{uriResult.ToString().Trim('/')}?search[sortAscending]=false&search[sortBy]=CREATED_DATE";
                    await page.GotoAsync(collectionUrl);
                    var countSelector = page.Locator(".AssetSearchView--results-count");
                    var countText = await countSelector.InnerTextAsync();
                    await page.SetViewportSizeAsync(3200, 3200);
                    totalCount = Convert.ToInt32(countText.GetNumbers());
                    StatusLabel.Text = $"{totalCount} items found...";

                    //show small gallery
                    await page.ClickAsync(@"[value=""apps""]");

                    collectionFolder = Path.Combine(downloadFolder, uriResult.LocalPath.Replace("collection/", "").Trim('/'));
                    Directory.CreateDirectory(collectionFolder);
                    await CollectUrls();
                }
                else
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
            }
        }
        catch (Exception ex)
        {
            StatusLabel.Text = ex.Message;
            await Task.Delay(1500);
            await Reset();
        }
    }

    private void QualitySlider_ValueChanged(object sender, ValueChangedEventArgs e)
    {
        var newVal = Math.Round(e.NewValue);
        if (QualitySlider != null)
        {
            QualitySlider.Value = newVal;
            downloadSuffix = newVal switch
            {
                0 => "=w200",
                1 => "=w800",
                2 => "=w9999",
                _ => "=w200"
            };
        }
    }

    private async void OpenDownloadsButton_Clicked(object sender, EventArgs e)
    {
#if WINDOWS
        var directory = string.IsNullOrEmpty(collectionFolder) ? downloadFolder : collectionFolder;
        await Windows.System.Launcher.LaunchFolderPathAsync(directory);
#endif
    }

    private void SettingsButton_Clicked(object sender, EventArgs e)
    {
        if (!LoadingGrid.IsVisible)
        {
            SettingsGrid.IsVisible = !SettingsGrid.IsVisible;
        }
    }
}