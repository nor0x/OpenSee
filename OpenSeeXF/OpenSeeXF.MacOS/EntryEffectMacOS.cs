using System;
using AppKit;
using OpenSeeXF.MacOS;
using Xamarin.Forms;
using Xamarin.Forms.Platform.MacOS;


[assembly: ResolutionGroupName("OpenSeeXF")]
[assembly: ExportEffect(typeof(EntryEffect), nameof(EntryEffect))]
namespace OpenSeeXF.MacOS
{
    public class EntryEffect : PlatformEffect
    {


        protected override void OnAttached()
        {
            if(Control is NSTextField textField)
            {
                textField.FocusRingType = NSFocusRingType.None;
                textField.UsesSingleLineMode = true;
            }
        }

        protected override void OnDetached()
        {
            throw new NotImplementedException();
        }
    }
}
