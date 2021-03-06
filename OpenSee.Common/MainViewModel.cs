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
using System.Diagnostics;
using System.Windows.Input;

#if MACOS
using Dasync.Collections;
#endif


namespace OpenSee.Common
{
    public partial class MainViewModel : ObservableRecipient
    {
        IPlaywright _playwright;
        IPage _page;
        string _downloadFolder;
        string _collectionFolder;
        bool _browserReady;
        bool _downloadRequested;
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
                SetProperty(ref _qualityValue, newVal);
            }
        }

        public ObservableCollection<string> AllUrls;

        int _totalCount;
        public int TotalCount
        {
            get => _totalCount;
            set => SetProperty(ref _totalCount, value);
        }

        string _statusText;
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        string _url;
        public string Url
        {
            get => _url;
            set => SetProperty(ref _url, value);
        }

        double _progress;
        public double Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        bool _showLoadingGrid;
        public bool ShowLoadingGrid
        {
            get => _showLoadingGrid;
            set => SetProperty(ref _showLoadingGrid, value);
        }

        bool _showSettingsGrid;
        public bool ShowSettingsGrid
        {
            get => _showSettingsGrid;
            set => SetProperty(ref _showSettingsGrid, value);
        }

        bool _isDownloading;
        public bool IsDownloading
        {
            get => _isDownloading;
            set => SetProperty(ref _isDownloading, value);
        }

        public ICommand ToggleSettingsCommand { get; }
        public ICommand StartDownloadCommand { get; }
        public ICommand OpenDownloadsFolderCommand { get; }

        public MainViewModel()
        {
            _downloadFolder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            _downloadUrls = new List<string>();
            AllUrls = new ObservableCollection<string>();
            QualityValue = 1.0;
            ToggleSettingsCommand = new RelayCommand(ToggleSettings);
            StartDownloadCommand = new AsyncRelayCommand(StartDownload);
            OpenDownloadsFolderCommand = new RelayCommand(OpenDownloadsFolder);
        }

        public async Task Init()
        {
            try
            {
                _playwright = await Playwright.CreateAsync();
#if MACOS
                _browserReady = File.Exists(_playwright.Webkit.ExecutablePath);
#endif
#if WINDOWS 
                _browserReady = File.Exists(_playwright.Firefox.ExecutablePath);
#endif
                if (!_browserReady)
                {
                    StatusText = "warming up...";
                    await Task.Run(() =>
                    {
#if MACOS
                        Microsoft.Playwright.Program.Main(new string[] { "install", "webkit" });
#endif
#if WINDOWS
                        Microsoft.Playwright.Program.Main(new string[] { "install", "firefox" });
#endif
                        _browserReady = true;
                        StatusText = "ready!";

                        if (_downloadRequested)
                        {
                            StartDownload();
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                StatusText = ex.Message;
            }
        }

        void ToggleSettings()
        {
            if (!ShowLoadingGrid)
            {
                ShowSettingsGrid = !ShowSettingsGrid;
            }
        }

        private async Task StartDownload()
        {
            try
            {
                if (_isDownloading)
                {
                    await Reset();
                }
                else
                {
                    ShowLoadingGrid = true;
                    ShowSettingsGrid = false;
                    if (!_browserReady)
                    {
                        _downloadRequested = true;
                        return;
                    }
                    StatusText = "looking for collection...";

                    _isDownloading = true;
                    Uri uriResult;
                    if (Uri.TryCreate(Url, UriKind.Absolute, out uriResult) || Url?.ToLower().Contains("opensea.io") == false)
                    {
#if MACOS
                        await using var browser = await _playwright.Webkit.LaunchAsync(new() { Headless = true });
#endif
#if WINDOWS
                        await using var browser = await _playwright.Firefox.LaunchAsync(new() { Headless = true });
#endif
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
                        WeakReferenceMessenger.Default.Send(new ToggleAnimationMessage(true));

                        await CollectUrls();
                    }
                    else
                    {
                        WeakReferenceMessenger.Default.Send(new UrlValidMessage(false));
                        await Task.Delay(1500);
                        await Reset();
                    }
                }
            }
            catch (Exception ex)
            {
                WeakReferenceMessenger.Default.Send(new UrlValidMessage(false));
                StatusText = ex.Message;
                await Task.Delay(1500);
                await Reset();
            }
        }

        async Task CollectUrls()
        {
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
#if MACOS
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
#if WINDOWS

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

                        _downloadUrls.Add(source);
                        Progress = (double)(_downloadUrls.Count()) / _totalCount;

                        StatusText = Math.Round(Progress * 100) + "%";
                    }
                }
            });
#endif
        }

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
            _progress = 0;
            StatusText = "";
            ShowLoadingGrid = false;
            ShowSettingsGrid = false;
            WeakReferenceMessenger.Default.Send(new ToggleAnimationMessage(false));
        }
    }
}
