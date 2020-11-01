﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Atlasd.Localization {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Atlasd.Localization.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to That account is already logged on..
        /// </summary>
        public static string AccountIsAlreadyLoggedOn {
            get {
                return ResourceManager.GetString("AccountIsAlreadyLoggedOn", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {user} was disconnected..
        /// </summary>
        public static string AdminDisconnectCommand {
            get {
                return ResourceManager.GetString("AdminDisconnectCommand", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Forcefully moved {user} to the channel {channel}..
        /// </summary>
        public static string AdminMoveUserCommand {
            get {
                return ResourceManager.GetString("AdminMoveUserCommand", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An {exception} occurred while reloading the server configuration. Server stability is now at risk, notify a system administrator immediately..
        /// </summary>
        public static string AdminReloadCommandFailure {
            get {
                return ResourceManager.GetString("AdminReloadCommandFailure", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The server configuration has been reloaded..
        /// </summary>
        public static string AdminReloadCommandSuccess {
            get {
                return ResourceManager.GetString("AdminReloadCommandSuccess", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The server shutdown was cancelled..
        /// </summary>
        public static string AdminShutdownCommandCancelled {
            get {
                return ResourceManager.GetString("AdminShutdownCommandCancelled", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The server shutdown was scheduled..
        /// </summary>
        public static string AdminShutdownCommandScheduled {
            get {
                return ResourceManager.GetString("AdminShutdownCommandScheduled", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Set {user}&apos;s active channel flags to {flags}..
        /// </summary>
        public static string AdminSpoofUserFlagsCommand {
            get {
                return ResourceManager.GetString("AdminSpoofUserFlagsCommand", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unable to parse the flags into an unsigned 32-bit integer.
        ///Supported number prefixes:
        ///- Binary: 0b, 0B, &amp;b, &amp;B
        ///- Hex: 0x, 0X, &amp;h, &amp;H
        ///- Octal: 0
        ///- Decimal: Any whole number, not prefixed with zero (octal indication)
        ///Example: 0x10 = 16 decimal.
        /// </summary>
        public static string AdminSpoofUserFlagsCommandBadNumber {
            get {
                return ResourceManager.GetString("AdminSpoofUserFlagsCommandBadNumber", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You are no longer marked as away..
        /// </summary>
        public static string AwayCommandOff {
            get {
                return ResourceManager.GetString("AwayCommandOff", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You are now marked as being away..
        /// </summary>
        public static string AwayCommandOn {
            get {
                return ResourceManager.GetString("AwayCommandOn", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {user} is away ({awayMessage}).
        /// </summary>
        public static string AwayCommandStatus {
            get {
                return ResourceManager.GetString("AwayCommandStatus", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You are away ({awayMessage}).
        /// </summary>
        public static string AwayCommandStatusSelf {
            get {
                return ResourceManager.GetString("AwayCommandStatusSelf", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Backstage.
        /// </summary>
        public static string Backstage {
            get {
                return ResourceManager.GetString("Backstage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Welcome to {realm}!
        ///This server is hosted by {host}.
        ///There are currently {gameUsers} users playing {gameAds} games of {game}, and {totalUsers} users playing {totalGameAds} games on {realm}..
        /// </summary>
        public static string ChannelFirstJoinGreeting {
            get {
                return ResourceManager.GetString("ChannelFirstJoinGreeting", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to This channel does not have chat privileges..
        /// </summary>
        public static string ChannelIsChatRestricted {
            get {
                return ResourceManager.GetString("ChannelIsChatRestricted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to That channel is full..
        /// </summary>
        public static string ChannelIsFull {
            get {
                return ResourceManager.GetString("ChannelIsFull", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to That channel is restricted..
        /// </summary>
        public static string ChannelIsRestricted {
            get {
                return ResourceManager.GetString("ChannelIsRestricted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to That channel does not exist.
        ///(If you are trying to search for a user, use the /whois command.).
        /// </summary>
        public static string ChannelNotFound {
            get {
                return ResourceManager.GetString("ChannelNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to That character is not logged on.  Try using *username to message a user or character@realm to message a character in a different realm..
        /// </summary>
        public static string CharacterNotLoggedOn {
            get {
                return ResourceManager.GetString("CharacterNotLoggedOn", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to That command is not available at the moment..
        /// </summary>
        public static string ChatCommandUnavailable {
            get {
                return ResourceManager.GetString("ChatCommandUnavailable", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You have been disconnected by a system administrator..
        /// </summary>
        public static string DisconnectedByAdmin {
            get {
                return ResourceManager.GetString("DisconnectedByAdmin", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Disconnected by Admin.
        /// </summary>
        public static string DisconnectedByAdminCaption {
            get {
                return ResourceManager.GetString("DisconnectedByAdminCaption", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You have been disconnected by a system administrator ({reason})..
        /// </summary>
        public static string DisconnectedByAdminWithReason {
            get {
                return ResourceManager.GetString("DisconnectedByAdminWithReason", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error.
        /// </summary>
        public static string ErrorCaption {
            get {
                return ResourceManager.GetString("ErrorCaption", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed logon attempts since then: {count}.
        /// </summary>
        public static string FailedLogonAttempts {
            get {
                return ResourceManager.GetString("FailedLogonAttempts", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Chatting with this game is restricted to the channels listed in the channel menu..
        /// </summary>
        public static string GameProductIsChatRestricted {
            get {
                return ResourceManager.GetString("GameProductIsChatRestricted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Advanced chat commands:
        ////ban /beep /clan /designate /friends /kick /mail
        ////nobeep /options /rejoin /stats /time /unban /users
        ///Type  /help COMMAND  for information about a specific command..
        /// </summary>
        public static string HelpCommandAdvancedRemarks {
            get {
                return ResourceManager.GetString("HelpCommandAdvancedRemarks", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Command aliases:
        ///  /?         -- /help
        ///  /channel   -- /join
        ///  /squelch   -- /ignore
        ///  /whisper   -- /w /m /msg
        ///  /emote     -- /me
        ///  /rejoin    -- /resign
        ///  /unsquelch -- /unignore
        ///  /whois     -- /where /whereis
        ///  /friends   -- /f
        ///  /clan      -- /c
        ///Type  /help COMMAND  for information about a specific command..
        /// </summary>
        public static string HelpCommandAliasesRemarks {
            get {
                return ResourceManager.GetString("HelpCommandAliasesRemarks", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to /ban USERNAME  (see also: /unban)
        ///(For channel Operators only) Bans a USERNAME from the channel, and prevents him from returning.
        ///Example: /ban JoeUser.
        /// </summary>
        public static string HelpCommandBanRemarks {
            get {
                return ResourceManager.GetString("HelpCommandBanRemarks", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Basic chat commands:
        ////away /dnd /channel /emote 
        ////friends /options /squelch /unsquelch 
        ////whisper /who /whoami /whois
        ///Type /help COMMAND for information about a specific command..
        /// </summary>
        public static string HelpCommandCommandsRemarks {
            get {
                return ResourceManager.GetString("HelpCommandCommandsRemarks", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to /channel CHANNEL  (alias: /join)
        ///Moves you to a different CHANNEL.
        ///Example: /channel Blizzard Tech Support.
        /// </summary>
        public static string HelpCommandJoinRemarks {
            get {
                return ResourceManager.GetString("HelpCommandJoinRemarks", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Battle.net help topics:
        ///commands  aliases  advanced
        ///Type /help TOPIC for information about a specific topic..
        /// </summary>
        public static string HelpCommandRemarks {
            get {
                return ResourceManager.GetString("HelpCommandRemarks", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to /time
        ///Displays the current {realm} Time ({realmTimezone})..
        /// </summary>
        public static string HelpCommandTimeRemarks {
            get {
                return ResourceManager.GetString("HelpCommandTimeRemarks", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Invalid channel name..
        /// </summary>
        public static string InvalidChannelName {
            get {
                return ResourceManager.GetString("InvalidChannelName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to That is not a valid command. Type /help or /? for more info..
        /// </summary>
        public static string InvalidChatCommand {
            get {
                return ResourceManager.GetString("InvalidChatCommand", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Invalid user..
        /// </summary>
        public static string InvalidUser {
            get {
                return ResourceManager.GetString("InvalidUser", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Last logon: {timestamp}.
        /// </summary>
        public static string LastLogonInfo {
            get {
                return ResourceManager.GetString("LastLogonInfo", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No one hears you..
        /// </summary>
        public static string NoOneHearsYou {
            get {
                return ResourceManager.GetString("NoOneHearsYou", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to A server shutdown has been rescheduled in {period}..
        /// </summary>
        public static string ServerShutdownRescheduled {
            get {
                return ResourceManager.GetString("ServerShutdownRescheduled", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to A server shutdown has been rescheduled in {period} ({message})..
        /// </summary>
        public static string ServerShutdownRescheduledWithMessage {
            get {
                return ResourceManager.GetString("ServerShutdownRescheduledWithMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to A server shutdown has been scheduled in {period}..
        /// </summary>
        public static string ServerShutdownScheduled {
            get {
                return ResourceManager.GetString("ServerShutdownScheduled", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to A server shutdown has been scheduled in {period} ({message})..
        /// </summary>
        public static string ServerShutdownScheduledWithMessage {
            get {
                return ResourceManager.GetString("ServerShutdownScheduledWithMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The Void.
        /// </summary>
        public static string TheVoid {
            get {
                return ResourceManager.GetString("TheVoid", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {realm} time: {realmTime}
        ///Your local time: {localTime}.
        /// </summary>
        public static string TimeCommand {
            get {
                return ResourceManager.GetString("TimeCommand", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {target} was banned by {source}.
        /// </summary>
        public static string UserBannedFromChannel {
            get {
                return ResourceManager.GetString("UserBannedFromChannel", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {character}#{characterRealm} (*{username}) is using {game} in a private game..
        /// </summary>
        public static string UserIsUsingDiablo2InAPrivateGame {
            get {
                return ResourceManager.GetString("UserIsUsingDiablo2InAPrivateGame", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {username} is using {game} in a private game..
        /// </summary>
        public static string UserIsUsingGameInAPrivateGame {
            get {
                return ResourceManager.GetString("UserIsUsingGameInAPrivateGame", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {user} is using {game} in {realm}.
        /// </summary>
        public static string UserIsUsingGameInRealm {
            get {
                return ResourceManager.GetString("UserIsUsingGameInRealm", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {user} is using {game} in the channel {channel}..
        /// </summary>
        public static string UserIsUsingGameInTheChannel {
            get {
                return ResourceManager.GetString("UserIsUsingGameInTheChannel", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {username} is using {game} in game {gameAd}..
        /// </summary>
        public static string UserIsUsingGameInTheGame {
            get {
                return ResourceManager.GetString("UserIsUsingGameInTheGame", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {username} is using {game} in the password-protected game {gameAd}..
        /// </summary>
        public static string UserIsUsingGameInThePasswordedGame {
            get {
                return ResourceManager.GetString("UserIsUsingGameInThePasswordedGame", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {target} was kicked out of the channel by {source}..
        /// </summary>
        public static string UserKickedFromChannel {
            get {
                return ResourceManager.GetString("UserKickedFromChannel", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {target} was kicked out of the channel by {source} ({reason})..
        /// </summary>
        public static string UserKickedFromChannelWithReason {
            get {
                return ResourceManager.GetString("UserKickedFromChannelWithReason", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to That user is not in a channel..
        /// </summary>
        public static string UserNotInChannel {
            get {
                return ResourceManager.GetString("UserNotInChannel", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to That user is not logged on..
        /// </summary>
        public static string UserNotLoggedOn {
            get {
                return ResourceManager.GetString("UserNotLoggedOn", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {target} was unbanned by {source}.
        /// </summary>
        public static string UserUnBannedFromChannel {
            get {
                return ResourceManager.GetString("UserUnBannedFromChannel", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Users in channel {channel}:
        ///{users}.
        /// </summary>
        public static string WhoCommand {
            get {
                return ResourceManager.GetString("WhoCommand", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You are banned from that channel..
        /// </summary>
        public static string YouAreBannedFromThatChannel {
            get {
                return ResourceManager.GetString("YouAreBannedFromThatChannel", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You are not a channel operator..
        /// </summary>
        public static string YouAreNotAChannelOperator {
            get {
                return ResourceManager.GetString("YouAreNotAChannelOperator", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You are {name}, using {game} in game {gameAd}..
        /// </summary>
        public static string YouAreUsingGameInGame {
            get {
                return ResourceManager.GetString("YouAreUsingGameInGame", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You are {name}, using {game} in {realm}.
        /// </summary>
        public static string YouAreUsingGameInRealm {
            get {
                return ResourceManager.GetString("YouAreUsingGameInRealm", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You are {name}, using {game} in the channel {channel}..
        /// </summary>
        public static string YouAreUsingGameInTheChannel {
            get {
                return ResourceManager.GetString("YouAreUsingGameInTheChannel", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You can&apos;t kick a channel operator..
        /// </summary>
        public static string YouCannotKickAChannelOperator {
            get {
                return ResourceManager.GetString("YouCannotKickAChannelOperator", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Your friend {friend} has entered a {game} game called {gameAd}..
        /// </summary>
        public static string YourFriendEnteredGame {
            get {
                return ResourceManager.GetString("YourFriendEnteredGame", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Your friend {friend} has entered a password-protected {game} game called {gameAd}..
        /// </summary>
        public static string YourFriendEnteredPasswordedGame {
            get {
                return ResourceManager.GetString("YourFriendEnteredPasswordedGame", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Your friend {friend} has exited {realm}..
        /// </summary>
        public static string YourFriendLoggedOff {
            get {
                return ResourceManager.GetString("YourFriendLoggedOff", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Your friend {friend} has entered {realm}..
        /// </summary>
        public static string YourFriendLoggedOn {
            get {
                return ResourceManager.GetString("YourFriendLoggedOn", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {source} kicked you out of the channel!.
        /// </summary>
        public static string YouWereKickedFromChannel {
            get {
                return ResourceManager.GetString("YouWereKickedFromChannel", resourceCulture);
            }
        }
    }
}
