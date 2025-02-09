using System;
using System.IO;
using System.Windows;

namespace GoldbergGUI.Core.Utils
{
    public class Secrets : ISecrets
    {
        public string SteamWebApiKey()
        {
            string apiKey = PromptDialog.Prompt("Enter your Steam Web API Key below:", "Steam Web API Key", "You can get a Steam Web API Key at https://steamcommunity.com/dev/apikey");
            if (string.IsNullOrEmpty(apiKey))
            {
                MessageBox.Show("You must enter a Steam Web API Key to use this application.", "Steam Web API Key Required", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
                return null;
            }
            else
            {
                return apiKey;
            }
            
        }
    }
}

namespace GoldbergGUI.Core.Utils
{
    public interface ISecrets
    {
        public string SteamWebApiKey();
    }
}