// Create a "Secrets" class, implement "ISecrets" interface and add your keys:

using System;

namespace GoldbergGUI.Core.Utils
{
    public class Secrets : ISecrets
    {
        public string SteamWebApiKey()
        {
            return "<ENTER STEAM WEB API KEY HERE>";
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