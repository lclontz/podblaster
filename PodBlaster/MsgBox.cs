using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.UI.Popups;

namespace PodBlaster
{
    public static class MsgBox
    {
        static public async void Show(string mytext)
        {
            var dialog = new MessageDialog(mytext, "Test Message");
            await dialog.ShowAsync();

        }


    }
}
