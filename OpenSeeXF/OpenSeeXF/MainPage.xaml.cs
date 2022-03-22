using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using OpenSee.Common;
using OpenSee.Common.Helpers;
using Xamarin.Forms;

namespace OpenSeeXF
{
    public partial class MainPage : ContentPage
    {

        bool _entryAnimating;
        bool _imageAnimating;

        Random random;

        MainViewModel ViewModel => BindingContext as MainViewModel;

        public MainPage()
        {
            InitializeComponent();
            random = new Random();
            CurrentImage.PropertyChanged += CurrentImage_PropertyChanged;

            WeakReferenceMessenger.Default.Register<UrlValidMessage>(this, async (r, m) =>
            {
                AnimateEntry();
            });

            WeakReferenceMessenger.Default.Register<ToggleAnimationMessage>(this, (r, m) =>
            {
                if (m.Value)
                {
                    StartAnimatingImage();
                }
                else
                {
                    this.AbortAnimation("DownloadImageAnimation");
                }
            });
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await ViewModel.Init();
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
                        CurrentImage.Source = ViewModel.AllUrls.GetRandomElement();
                    });
                });
            }
        }
    }
}

