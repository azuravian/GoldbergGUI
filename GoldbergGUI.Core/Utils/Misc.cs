namespace GoldbergGUI.Core.Utils
{
    public static class Misc
    {
        public const string AlphaNumOnlyRegex = "[^0-9a-zA-Z]+";
    }

    public class GlobalHelp
    {
        public static string Header =>
            "Information\n";

        public static string TextPreLink =>
            "Usually these settings are saved under";

        public static string Link => "%APPDATA%\\Goldberg SteamEmu Saves\\settings";

        public static string TextPostLink =>
            ", which makes these " +
            "available for every game that uses the Goldberg Emulator. However, if you want to set specific settings " +
            "for certain games (e.g. different language), you can remove the \"Global\" checkmark next to the option " +
            "and then change it. If you want to remove that setting, just empty the field while \"Global\" is " +
            "unchecked. (Not implemented yet!)";
    }
}