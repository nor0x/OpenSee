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

            WeakReferenceMessenger.Default.Register<UrlValidMessage>(this, async (r, m) =>
            {
                Console.WriteLine("url valid? " + m.Value);
                AnimateEntry();
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

        void Button_Clicked(System.Object sender, System.EventArgs e)
        {
            Console.WriteLine("url is: " + ViewModel.Url);
        }

    }
}

