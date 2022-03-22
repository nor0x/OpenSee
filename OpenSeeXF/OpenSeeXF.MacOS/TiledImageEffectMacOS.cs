using System;
using AppKit;
using OpenSeeXF.MacOS;
using Xamarin.Forms;
using Xamarin.Forms.Platform.MacOS;


[assembly: ExportEffect(typeof(TiledImageEffect), nameof(TiledImageEffect))]
namespace OpenSeeXF.MacOS
{
    public class TiledImageEffect : PlatformEffect
    {
        protected override void OnAttached()
        {
            if(Control is NSImageView imgView)
            {
                var c = NSColor.FromPatternImage(imgView.Image);
                imgView.Image = null;
                Control.NeedsDisplay = true;
                Control.WantsLayer = true;
                Control.Layer.BackgroundColor = c.CGColor;
            }
        }

        protected override void OnDetached()
        {
            throw new NotImplementedException();
        }
    }
}
