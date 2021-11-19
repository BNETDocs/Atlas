# Atlas
**Atlas** is cross-platform software that emulates Classic Battle.net in a compatible model for Diablo, StarCraft, and WarCraft.

[![GitHub watchers](https://img.shields.io/github/watchers/BNETDocs/Atlas?style=for-the-badge)](https://github.com/BNETDocs/Atlas/watchers)
[![GitHub Repo stars](https://img.shields.io/github/stars/BNETDocs/Atlas?style=for-the-badge)](https://github.com/BNETDocs/Atlas/stargazers)
[![GitHub forks](https://img.shields.io/github/forks/BNETDocs/Atlas?style=for-the-badge)](https://github.com/BNETDocs/Atlas/network/members)
![GitHub contributors](https://img.shields.io/github/contributors/BNETDocs/Atlas?style=for-the-badge)

[![GitHub top language](https://img.shields.io/github/languages/top/BNETDocs/Atlas?style=for-the-badge)](https://github.com/BNETDocs/Atlas/search?l=c%23)
![GitHub code size in bytes](https://img.shields.io/github/languages/code-size/BNETDocs/Atlas?style=for-the-badge)
[![GitHub Workflow Status (branch)](https://img.shields.io/github/workflow/status/BNETDocs/Atlas/.NET%20Core/develop?style=for-the-badge)](https://github.com/BNETDocs/Atlas/actions?query=workflow%3A%22.NET%20Core%22)

[![GitHub All Releases](https://img.shields.io/github/downloads/BNETDocs/Atlas/total?style=for-the-badge)](https://github.com/BNETDocs/Atlas/releases/latest)
[![GitHub release (latest SemVer including pre-releases)](https://img.shields.io/github/v/release/BNETDocs/Atlas?include_prereleases&label=latest%20release&style=for-the-badge)](https://github.com/BNETDocs/Atlas/releases/latest)

## Authors

* [@carlbennett](https://github.com/carlbennett) a.k.a. Caaaaarrrrlll
* [@wjlafrance](https://github.com/wjlafrance) a.k.a. joe)x86(
* [@onemeandragon](https://github.com/OneMeanDragon) a.k.a. l)ragon
* Special thanks to: [BNETDocs](https://bnetdocs.org)
* Honorable mention to: Valhalla Legends (a.k.a. vL)

## Setup

### Requirements

Atlas is built on the .NET Core 3.1 platform. Linux users may wish to install the Mono package which provides a `dotnet` commandline.

### Download

Go to [Releases](https://github.com/BNETDocs/Atlas/releases/latest) and download the correct package for your platform, or build from source if you're savvy enough.

### Compile from source

#### Windows

Users on the Windows platform must install Microsoft Visual Studio 2019 (or equivalent) which provides .NET Core 3.1 development SDK.

#### Linux

For Fedora or other Red Hat based systems:

1. Install .NET: `sudo dnf install dotnet`
2. Clone this repository: `git clone https://github.com/BNETDocs/Atlas.git`
3. Initialize submodules: `git submodule update --init --recursive`
4. Change directory: `cd Atlas/src/Atlasd`
5. Build source: `dotnet build`
6. Run debugger: `dotnet run`
7. See `dotnet --help` for other compile options

### Adjust settings

1. Copy the `etc/atlasd.sample.json` to either the same directory or where you will store configs for this software. Name it whatever you wish, `atlasd.json` is a good example. Point the Atlasd daemon at the file using the `-c` or `--config` command-line argument; e.g. `atlasd -c ../etc/atlasd.json`
2. Change the settings as desired.
3. Launch atlasd.
4. If you find yourself wishing to change additional settings after atlasd has started, you may either restart atlasd, or send `/admin reload` as a command through a bot.

## License

Atlas is free software distributed under the [MIT License](./LICENSE.txt). It is not officially affiliated with or endorsed by Blizzard Entertainment, its subsidiaries, or business partners. Battle.net, Diablo, StarCraft, and WarCraft are registered trademarks of Blizzard Entertainment in the United States. This software is provided as-is in the hopes that it is useful without warranty of any kind.
