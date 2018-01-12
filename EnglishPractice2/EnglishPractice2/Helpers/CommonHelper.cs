using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishPractice2.Helpers
{
    public class CommonHelper
    {
        /// <summary>
        /// This HResult represents the scenario where a user is prompted to allow in-app speech, but 
        /// declines. This should only happen on a Phone device, where speech is enabled for the entire device,
        /// not per-app.
        /// </summary>
        public static uint HResultPrivacyStatementDeclined = 0x80045509;

        /// <summary>
        /// the HResult 0x8004503a typically represents the case where a recognizer for a particular language cannot
        /// be found. This may occur if the language is installed, but the speech pack for that language is not.
        /// See Settings -> Time & Language -> Region & Language -> *Language* -> Options -> Speech Language Options.
        /// </summary>
        public static uint HResultRecognizerNotFound = 0x8004503a;

        /// <summary>
        /// ShowMessageAsync
        /// </summary>
        /// <param name="message"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        public static async Task ShowMessageAsync(string message, string title = "Information")
        {
            var messageDialog = new Windows.UI.Popups.MessageDialog(message, title);
            await messageDialog.ShowAsync();
        }
    }
}
