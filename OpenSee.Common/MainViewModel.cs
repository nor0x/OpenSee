using Microsoft.Playwright;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OpenSee.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#if XAMARIN
using Dasync.Collections;
using Xamarin.Forms;
#endif


namespace OpenSee.Common
{
    [INotifyPropertyChanged]
    public partial class MainViewModel
    {
        IPlaywright _playwright;
        IPage _page;
        string _downloadFolder;
        string _collectionFolder;
        bool _browserReady;
        bool _downloadRequested;
        bool _isDownloading;
        List<string> _downloadUrls;

        string _downloadSuffix = "=w500";

        double _qualityValue;
        public double QualityValue
        {
            get => _qualityValue;
            set
            {
                var newVal = Math.Round(value);
                _downloadSuffix = newVal switch
                {
                    0 => "=w200",
                    1 => "=w800",
                    2 => "=w9999",
                    _ => "=w200"
                };
                _qualityValue = newVal;
            }
        }

        public ObservableCollection<string> AllUrls;

        [ObservableProperty]
        int _totalCount;

        [ObservableProperty]
        string _statusText;

        [ObservableProperty]
        string _url;

        [ObservableProperty]
        double _progress;

        [ObservableProperty]
        bool _showLoadingGrid;

        [ObservableProperty]
        bool _showSettingsGrid;

        public MainViewModel()
        {
            _downloadFolder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            _downloadUrls = new List<string>();
            AllUrls = new ObservableCollection<string>();
        }

        public async Task Init()
        {
            _playwright = await Playwright.CreateAsync();
            _browserReady = File.Exists(_playwright.Firefox.ExecutablePath);
            if (!_browserReady)
            {
                StatusText = "warming up...";
                await Task.Run(() =>
                {
                    Microsoft.Playwright.Program.Main(new string[] { "install", "firefox" });
                    _browserReady = true;
                    if (_downloadRequested)
                    {
                        StartDownload();
                    }

                });
            }
        }

        [ICommand]
        private async void StartDownload()
        {
            try
            {
                if (_isDownloading)
                {
                    await Reset();
                }
                else
                {
                    //DownloadButton.FontFamily = "TablerIcons";
                    //DownloadButton.Text = IconFont.x;
                    _showLoadingGrid = true;
                    _showSettingsGrid = false;
                    if (!_browserReady)
                    {
                        _downloadRequested = true;
                        return;
                    }
                    StatusText = "looking for collection...";

                    _isDownloading = true;
                    Uri uriResult;
                    Console.WriteLine("url: " + _url);
                    if (Uri.TryCreate(_url, UriKind.Absolute, out uriResult) && uriResult.Host.ToLower().Contains("opensea.io"))
                    {
                        await using var browser = await _playwright.Firefox.LaunchAsync(new() { Headless = true });
                        _page = await browser.NewPageAsync();
                        var collectionUrl = $"{uriResult.ToString().Trim('/')}?search[sortAscending]=false&search[sortBy]=CREATED_DATE";
                        await _page.GotoAsync(collectionUrl);
                        var countSelector = _page.Locator(".AssetSearchView--results-count");
                        var countText = await countSelector.InnerTextAsync();
                        await _page.SetViewportSizeAsync(3200, 3200);
                        _totalCount = Convert.ToInt32(countText.GetNumbers());
                        StatusText = $"{_totalCount} items found...";

                        //show small gallery
                        await _page.ClickAsync(@"[value=""apps""]");

                        _collectionFolder = Path.Combine(_downloadFolder, uriResult.LocalPath.Replace("collection/", "").Trim('/'));
                        Directory.CreateDirectory(_collectionFolder);
                        await CollectUrls();
                    }
                    else
                    {
                        WeakReferenceMessenger.Default.Send(new UrlValidMessage(false));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("exception: " + ex);
                StatusText = ex.Message;
                await Task.Delay(1500);
                await Reset();
            }
        }

        async Task CollectUrls()
        {
            //StartAnimatingImage();
            while (AllUrls.Count() != _totalCount)
            {
                var collectionHandle = await _page.QuerySelectorAsync(".AssetSearchView--results");
                var imgs = await collectionHandle.QuerySelectorAllAsync("img");
                await CheckImageSources(imgs);
                await _page.EvaluateAsync("window.scrollBy(0,document.body.scrollHeight)");
            }
            await Reset();
        }

        async Task CheckImageSources(IReadOnlyList<IElementHandle> handles)
        {


            using HttpClient client = new();
#if XAMARIN
            await handles.ParallelForEachAsync(
                async handle =>
                {
                    var sourceHandle = await handle.GetAttributeAsync("src");
                    var source = sourceHandle.ToString();
                    if (!source.Contains(".svg"))
                    {
                        if (!AllUrls.Contains(source))
                        {
                            AllUrls.Add(source);
                            var blankSource = source.Substring(0, source.IndexOf("=w"));

                            var bytes = await client.GetByteArrayAsync(blankSource + _downloadSuffix);
                            await File.WriteAllBytesAsync(Path.Combine(_collectionFolder, Guid.NewGuid().ToString("N") + ".png"), bytes);

                            _downloadUrls.Add(source);
                            Progress = (double)(_downloadUrls.Count()) / _totalCount;
                            StatusText = Math.Round(_progress * 100) + "%";
                        }
                    }
                },
                maxDegreeOfParallelism: 5,
                cancellationToken: CancellationToken.None);
#endif
#if MAUI

            ParallelOptions parallelOptions = new()
            {
                MaxDegreeOfParallelism = 5
            };

            await Parallel.ForEachAsync(handles, parallelOptions, async (handle, token) =>
            {
                var sourceHandle = await handle.GetAttributeAsync("src");
                var source = sourceHandle.ToString();
                if (!source.Contains(".svg"))
                {
                    if (!AllUrls.Contains(source))
                    {
                        AllUrls.Add(source);
                        var blankSource = source.Substring(0, source.IndexOf("=w"));

                        var bytes = await client.GetByteArrayAsync(blankSource + _downloadSuffix);
                        await File.WriteAllBytesAsync(Path.Combine(_collectionFolder, Guid.NewGuid().ToString("N") + ".png"), bytes);

                        downloadUrls.Add(source);
                        var progress = (double)(downloadUrls.Count()) / _totalCount;
                        await DownloadProgress.ProgressTo(progress, 100, Easing.CubicOut);
                        
                        StatusText = Math.Round(progress * 100) + "%";
                    }
                }
            });

#endif
        }

        [ICommand]
        void OpenDownloadsFolder()
        {
            var directory = string.IsNullOrEmpty(_collectionFolder) ? _downloadFolder : _collectionFolder;
            WeakReferenceMessenger.Default.Send(new OpenFolderMessage(directory));
        }

        async Task Reset()
        {
            _isDownloading = false;
            _totalCount = 0;
            AllUrls.Clear();
            _downloadUrls.Clear();
            if (_page != null)
            {
                await _page?.CloseAsync();
            }
            //DownloadButton.FontFamily = "Lato";
            //DownloadButton.Text = "Download";
            _progress = 0;
            StatusText = "";
            _showLoadingGrid = false;
            _showSettingsGrid = false;
        }

    }
}
