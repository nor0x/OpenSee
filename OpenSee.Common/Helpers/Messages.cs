using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenSee.Common.Helpers
{
    public class OpenFolderMessage : ValueChangedMessage<string>
    {
        public OpenFolderMessage(string path) : base(path)
        {
        }
    }

    public class UrlValidMessage : ValueChangedMessage<bool>
    {
        public UrlValidMessage(bool valid) : base(valid)
        {
        }
    }

    public class ToggleAnimationMessage : ValueChangedMessage<bool>
    {
        public ToggleAnimationMessage(bool toggle) : base(toggle)
        {
        }
    }
}
