using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishPractice.Helpers
{
    public static class CommonHelper 
    {
        public static async Task ShowMessageAsync(string message, string title = "Information")
        {
            var messageDialog = new Windows.UI.Popups.MessageDialog(message, title);
            await messageDialog.ShowAsync();
        }
    }
}
