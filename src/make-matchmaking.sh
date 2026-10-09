#!/usr/bin/env bash
# Writes the Warcraft III demo's game type list (matchmaking-w3dm-enUS.dat).
#
# usage: make-matchmaking.sh [output file]
#
# The demo downloads this file over BNFTP after logging on and reads it with
# CBattleNetGlueData; it must be consumed exactly. Layout, all integers
# little-endian:
#
#   (UINT32) Header
#    (UINT8) Number of game types
#   For each game type:
#      (UINT8) Game type id (the demo recognizes 0, 2, 3 and 4)
#      (UINT8) Number of styles
#     For each style:
#        (UINT8) Style id
#       (STRING) Name, at most 127 characters
#        (UINT8) Number of maps
#       For each map:
#         (STRING) Map path relative to the game directory, at most 259 characters
#         (UINT32) Unknown
#       (STRING) Description, at most 511 characters
#
# Each map path must name a map the client has installed, or the demo lists it
# as missing or corrupt and disables Play Game.

set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
output="${1:-$root/var/bnftp/matchmaking-w3dm-enUS.dat}"

work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT

cat > "$work/make-matchmaking.cs" <<'CSHARP'
using System.Text;

var maps = new[] { (Path: @"Maps\(4)Deadlock.w3m", Unknown: 0u) };

var gameTypes = new[]
{
    (Id: (byte)0, Styles: new[] { (Id: (byte)0, Name: "Solo", Maps: maps, Description: "One versus one") }),
    (Id: (byte)2, Styles: new[] { (Id: (byte)0, Name: "2 vs 2", Maps: maps, Description: "Two versus two") }),
    (Id: (byte)3, Styles: new[] { (Id: (byte)0, Name: "3 vs 3", Maps: maps, Description: "Three versus three") }),
    (Id: (byte)4, Styles: new[] { (Id: (byte)0, Name: "4 vs 4", Maps: maps, Description: "Four versus four") }),
};

using var stream = new MemoryStream();
using var writer = new BinaryWriter(stream);

void WriteString(string value)
{
    writer.Write(Encoding.ASCII.GetBytes(value));
    writer.Write((byte)0);
}

writer.Write(1u);
writer.Write((byte)gameTypes.Length);

foreach (var gameType in gameTypes)
{
    writer.Write(gameType.Id);
    writer.Write((byte)gameType.Styles.Length);

    foreach (var style in gameType.Styles)
    {
        writer.Write(style.Id);
        WriteString(style.Name);
        writer.Write((byte)style.Maps.Length);

        foreach (var map in style.Maps)
        {
            WriteString(map.Path);
            writer.Write(map.Unknown);
        }

        WriteString(style.Description);
    }
}

File.WriteAllBytes(args[0], stream.ToArray());
Console.WriteLine($"{stream.Length} bytes written to {args[0]}");
CSHARP

dotnet run "$work/make-matchmaking.cs" -- "$output"
