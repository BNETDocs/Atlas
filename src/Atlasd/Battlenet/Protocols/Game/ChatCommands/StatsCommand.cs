using Atlasd.Localization;
using System;
using System.Collections.Generic;
using System.Globalization;
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

            if (Arguments.Count < 1)
            {
                new ChatEvent(ChatEvent.EventIds.EID_ERROR, gs.ChannelFlags, gs.Client.RemoteIPAddress, gs.Ping, gs.OnlineName, Resources.StatsCommandInvalid).WriteTo(gs.Client);
                return;
            }

            var targetStr = Arguments[0];
            Arguments.RemoveAt(0);
            // Calculates and removes (targetStr+' ') from (raw) which prints into (newRaw):
            RawBuffer = RawBuffer[(Encoding.UTF8.GetByteCount(targetStr) + (Arguments.Count > 0 ? 1 : 0))..];

            Product.ProductCode code = Product.ProductCode.None;
            if (Arguments.Count == 0)
            {
                code = gs.Product;
            }
            else if (Arguments.Count > 0)
            {
                var codeStr = Arguments[0];
                Arguments.RemoveAt(0);
                // Calculates and removes (codeStr+' ') from (raw) which prints into (newRaw):
                RawBuffer = RawBuffer[(Encoding.UTF8.GetByteCount(codeStr) + (Arguments.Count > 0 ? 1 : 0))..];

                while (codeStr.Length < 4) codeStr += 0x00;
                code = Product.FromBytes(Encoding.UTF8.GetBytes(codeStr[0..Math.Min(4, codeStr.Length)]), true);
            }

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
                new ChatEvent(ChatEvent.EventIds.EID_ERROR, gs.ChannelFlags, gs.Client.RemoteIPAddress, gs.Ping, gs.OnlineName, Resources.StatsCommandInvalid).WriteTo(gs.Client);
                return;
            }

            var buffer = Resources.StatsCommand;
            buffer += Battlenet.Common.NewLine + Resources.StatsCommandNormal;
            buffer += Battlenet.Common.NewLine + Resources.StatsCommandLadder;
            if (code == Product.ProductCode.WarcraftIIBNE) buffer += Battlenet.Common.NewLine + Resources.StatsCommandIronMan;

            buffer = buffer.Replace("{ironManDraws}", "0", true, CultureInfo.InvariantCulture);
            buffer = buffer.Replace("{ironManLosses}", "0", true, CultureInfo.InvariantCulture);
            buffer = buffer.Replace("{ironManWins}", "0", true, CultureInfo.InvariantCulture);
            buffer = buffer.Replace("{ladderDraws}", "0", true, CultureInfo.InvariantCulture);
            buffer = buffer.Replace("{ladderLosses}", "0", true, CultureInfo.InvariantCulture);
            buffer = buffer.Replace("{ladderWins}", "0", true, CultureInfo.InvariantCulture);
            buffer = buffer.Replace("{normalDraws}", "0", true, CultureInfo.InvariantCulture);
            buffer = buffer.Replace("{normalLosses}", "0", true, CultureInfo.InvariantCulture);
            buffer = buffer.Replace("{normalWins}", "0", true, CultureInfo.InvariantCulture);
            buffer = buffer.Replace("{user}", targetStr, true, CultureInfo.InvariantCulture);

            foreach (var line in buffer.Split(Battlenet.Common.NewLine))
                new ChatEvent(ChatEvent.EventIds.EID_INFO, gs.ChannelFlags, gs.Client.RemoteIPAddress, gs.Ping, gs.OnlineName, line).WriteTo(gs.Client);
        }
    }
}
