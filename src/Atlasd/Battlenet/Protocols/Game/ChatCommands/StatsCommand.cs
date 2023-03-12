using Atlasd.Localization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Game.ChatCommands
{
    class StatsCommand : ChatCommand
    {
        public StatsCommand(byte[] rawBuffer, List<string> arguments) : base(rawBuffer, arguments) { }

        public override bool CanInvoke(ChatCommandContext context)
        {
            return context != null && context.GameState != null && context.GameState.ActiveAccount != null;
        }

        public override void Invoke(ChatCommandContext context)
        {
            var gs = context.GameState;
            var account = gs.ActiveAccount;

            if (Arguments.Count < 1)
            {
                foreach (var line in Resources.StatsCommandInvalid.Split(Battlenet.Common.NewLine))
                    new ChatEvent(ChatEvent.EventIds.EID_ERROR, gs.ChannelFlags, gs.Client.RemoteIPAddress, gs.Ping, gs.OnlineName, line).WriteTo(gs.Client);
                return;
            }

            var targetStr = Arguments[0];
            Arguments.RemoveAt(0);
            // Calculates and removes (targetStr+' ') from (raw) which prints into (newRaw):
            RawBuffer = RawBuffer[(Encoding.UTF8.GetByteCount(targetStr) + (Arguments.Count > 0 ? 1 : 0))..];

            Product.ProductCode code = Product.ProductCode.None;
            string codeStr = string.Empty;

            if (Arguments.Count == 0)
            {
                code = gs.Product;
            }
            else
            {
                codeStr = Arguments[0];
                Arguments.RemoveAt(0);
                // Calculates and removes (codeStr+' ') from (raw) which prints into (newRaw):
                RawBuffer = RawBuffer[(Encoding.UTF8.GetByteCount(codeStr) + (Arguments.Count > 0 ? 1 : 0))..];

                while (codeStr.Length < 4) codeStr += (char)0x00;
                code = Product.FromBytes(Encoding.UTF8.GetBytes(codeStr[0..Math.Min(4, codeStr.Length)]), true);
            }
            codeStr = Encoding.UTF8.GetString(BitConverter.GetBytes((uint)code).Reverse().ToArray());

            // Verify product code is a record-keeping type
            bool valid = code switch
            {
                Product.ProductCode.StarcraftOriginal => true,
                Product.ProductCode.StarcraftBroodwar => true,
                Product.ProductCode.WarcraftIIBNE => true,
                Product.ProductCode.WarcraftIIIReignOfChaos => true,
                Product.ProductCode.WarcraftIIIFrozenThrone => true,
                _ => false,
            };
            if (!valid)
            {
                foreach (var line in Resources.StatsCommandInvalid.Split(Battlenet.Common.NewLine))
                    new ChatEvent(ChatEvent.EventIds.EID_ERROR, gs.ChannelFlags, gs.Client.RemoteIPAddress, gs.Ping, gs.OnlineName, line).WriteTo(gs.Client);
                return;
            }

            var buffer = Resources.StatsCommand;
            buffer += Battlenet.Common.NewLine + Resources.StatsCommandNormal;
            buffer += Battlenet.Common.NewLine + Resources.StatsCommandLadder;
            if (code == Product.ProductCode.WarcraftIIBNE) buffer += Battlenet.Common.NewLine + Resources.StatsCommandIronMan;

            int normal = 0;
            int ladder = 1;
            int ironMan = 3;

            int normalWins = account.Get($"record\\{codeStr}\\{normal}\\wins", 0);
            int normalLosses = account.Get($"record\\{codeStr}\\{normal}\\losses", 0);
            int normalDraws = account.Get($"record\\{codeStr}\\{normal}\\disconnects", 0);
            int ladderWins = account.Get($"record\\{codeStr}\\{ladder}\\wins", 0);
            int ladderLosses = account.Get($"record\\{codeStr}\\{ladder}\\losses", 0);
            int ladderDraws = account.Get($"record\\{codeStr}\\{ladder}\\disconnects", 0);
            int ironManWins = account.Get($"record\\{codeStr}\\{ironMan}\\wins", 0);
            int ironManLosses = account.Get($"record\\{codeStr}\\{ironMan}\\losses", 0);
            int ironManDraws = account.Get($"record\\{codeStr}\\{ironMan}\\disconnects", 0);

            buffer = buffer.Replace("{ironManDraws}", $"{ironManDraws}", true, CultureInfo.InvariantCulture);
            buffer = buffer.Replace("{ironManLosses}", $"{ironManLosses}", true, CultureInfo.InvariantCulture);
            buffer = buffer.Replace("{ironManWins}", $"{ironManWins}", true, CultureInfo.InvariantCulture);
            buffer = buffer.Replace("{ladderDraws}", $"{ladderDraws}", true, CultureInfo.InvariantCulture);
            buffer = buffer.Replace("{ladderLosses}", $"{ladderLosses}", true, CultureInfo.InvariantCulture);
            buffer = buffer.Replace("{ladderWins}", $"{ladderWins}", true, CultureInfo.InvariantCulture);
            buffer = buffer.Replace("{normalDraws}", $"{normalDraws}", true, CultureInfo.InvariantCulture);
            buffer = buffer.Replace("{normalLosses}", $"{normalLosses}", true, CultureInfo.InvariantCulture);
            buffer = buffer.Replace("{normalWins}", $"{normalWins}", true, CultureInfo.InvariantCulture);
            buffer = buffer.Replace("{user}", targetStr, true, CultureInfo.InvariantCulture);
            buffer = buffer.Replace("{userName}", targetStr, true, CultureInfo.InvariantCulture);

            foreach (var line in buffer.Split(Battlenet.Common.NewLine))
                new ChatEvent(ChatEvent.EventIds.EID_INFO, gs.ChannelFlags, gs.Client.RemoteIPAddress, gs.Ping, gs.OnlineName, line).WriteTo(gs.Client);
        }
    }
}
