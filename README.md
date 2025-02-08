# GoldbergGUI

Set up any game with Goldberg's emulator easily and automatically.

## Installation

* Install the latest .NET Core Runtime by clicking
  [here](https://dotnet.microsoft.com/download/dotnet-core/current/runtime),
  clicking on the "Download x64" button and installing it.
* Download the latest release and extract it into any folder (e.g. %USERPROFILE%\Desktop\GoldbergGUI).

## Usage

* Double-click `GoldbergGUI.WPF.exe` to open the application.  
  _(When starting it for the first time, it might take a few seconds since it needs to
  cache a list of games available on the Steam Store and download the latest Goldberg Emulator release.)_
* Click on the "Select..." button on the top right and select the `steam_api.dll` or `steam_api64.dll` file in the game folder.
* Enter the name of the game and click on the "Find ID..." button.
  * If it did not find the right game, either try again with more precise keywords,
    or copy the app ID from the Steam Store to field to the right of the "Find ID..." button.
* Click the lower right "Get DLCs for AppID" button to fetch all available DLCs for the game.
* Set advanced options like "Offline mode" in the "Advanced" tab.
* Set global settings like account name and Steam64ID in the "Global Settings" tab.
* Click on "Save".

## Roadmap

While the most used options are available right now, I am planning to support all features of Goldberg's emulator, which include:

* Subscribed Groups
* Mods (Steam Workshop)
* Inventory and Items
* ~~Achievements~~
* Stats, Leaderboards
* Controller (Steam Input)

Apart from those, I'm also always looking into improving the user experience of the application and fixing any bugs.

## Acknowledgment

Goldberg Emulator is owned by Mr. Goldberg and licensed under the GNU Lesser General Public License v3.0.

### Contributors

* [UrbanCMC](https://github.com/UrbanCMC/) - Implementation of achievements

### Dependencies

* AngleSharp
* MvvmCross
* NinjaNye
* Serilog
* SharpCompress
* sqlite-net-pcl
* My fork of SteamStorefrontAPI

## License

GoldbergGUI is licensed under the GNU General Public License v3.0.
