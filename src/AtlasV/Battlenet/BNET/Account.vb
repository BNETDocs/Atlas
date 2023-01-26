Imports AtlasV.Daemon
Imports System
Imports System.Collections.Generic
Imports System.Net
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Text.Json

Namespace AtlasV.Battlenet
    Class Account
        Public Const AccountCreatedKey As String = "System\Account Created"
        Public Const FailedLogonsKey As String = "System\Total Failed Logons"
        Public Const FlagsKey As String = "System\Flags"
        Public Const FriendsKey As String = "System\Friends"
        Public Const IPAddressKey As String = "System\IP"
        Public Const LastLogoffKey As String = "System\Last Logoff"
        Public Const LastLogonKey As String = "System\Last Logon"
        Public Const PasswordKey As String = "System\Password Digest"
        Public Const PortKey As String = "System\Port"
        Public Const ProfileAgeKey As String = "profile\age"
        Public Const ProfileDescriptionKey As String = "profile\description"
        Public Const ProfileLocationKey As String = "profile\location"
        Public Const ProfileSexKey As String = "profile\sex"
        Public Const TimeLoggedKey As String = "System\Time Logged"
        Public Const UsernameKey As String = "System\Username"
        Public Const Alphanumeric As String = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"
        Public Const Punctuation As String = "`~!$%^&*()-_=+[{]}\|;:'"",<.>/?"

        Public Enum CreateStatus As UInt32
            Success = 0
            UsernameTooShort = 1
            UsernameInvalidChars = 2
            UsernameBannedWord = 3
            AccountExists = 4
            LastCreateInProgress = 5
            UsernameShortAlphanumeric = 6
            UsernameAdjacentPunctuation = 7
            UsernameTooManyPunctuation = 8
        End Enum

        <Flags>
        Public Enum Flags As UInt32
            None = &H0
            Employee = &H1
            ChannelOp = &H2
            Speaker = &H4
            Admin = &H8
            NoUDP = &H10
            Squelched = &H20
            Guest = &H40
            Closed = &H80
        End Enum

        Public Property Userdata As List(Of AccountKeyValue)

        Private Sub New()
            Userdata = New List(Of AccountKeyValue)() From {
                {New AccountKeyValue(AccountCreatedKey, DateTime.Now, AccountKeyValue.ReadLevel.Owner, AccountKeyValue.WriteLevel.[ReadOnly])},
                {New AccountKeyValue(FailedLogonsKey, CLng(0), AccountKeyValue.ReadLevel.Internal, AccountKeyValue.WriteLevel.Internal)},
                {New AccountKeyValue(FlagsKey, Flags.None, AccountKeyValue.ReadLevel.Internal, AccountKeyValue.WriteLevel.Internal)},
                {New AccountKeyValue(FriendsKey, New List(Of Byte())(), AccountKeyValue.ReadLevel.Internal, AccountKeyValue.WriteLevel.Internal)},
                {New AccountKeyValue(IPAddressKey, IPAddress.Any, AccountKeyValue.ReadLevel.Internal, AccountKeyValue.WriteLevel.Internal)},
                {New AccountKeyValue(LastLogoffKey, DateTime.Now, AccountKeyValue.ReadLevel.Owner, AccountKeyValue.WriteLevel.Internal)},
                {New AccountKeyValue(LastLogonKey, DateTime.Now, AccountKeyValue.ReadLevel.Owner, AccountKeyValue.WriteLevel.Internal)},
                {New AccountKeyValue(PortKey, 0, AccountKeyValue.ReadLevel.Internal, AccountKeyValue.WriteLevel.Internal)},
                {New AccountKeyValue(PasswordKey, Array.Empty(Of Byte)(), AccountKeyValue.ReadLevel.Internal, AccountKeyValue.WriteLevel.Internal)},
                {New AccountKeyValue(ProfileAgeKey, "", AccountKeyValue.ReadLevel.Any, AccountKeyValue.WriteLevel.Owner)},
                {New AccountKeyValue(ProfileDescriptionKey, "", AccountKeyValue.ReadLevel.Any, AccountKeyValue.WriteLevel.Owner)},
                {New AccountKeyValue(ProfileLocationKey, "", AccountKeyValue.ReadLevel.Any, AccountKeyValue.WriteLevel.Owner)},
                {New AccountKeyValue(ProfileSexKey, "", AccountKeyValue.ReadLevel.Any, AccountKeyValue.WriteLevel.Owner)},
                {New AccountKeyValue(TimeLoggedKey, CLng(0), AccountKeyValue.ReadLevel.Owner, AccountKeyValue.WriteLevel.Internal)},
                {New AccountKeyValue(UsernameKey, "", AccountKeyValue.ReadLevel.Any, AccountKeyValue.WriteLevel.Internal)}
            }
        End Sub

        Public Function ContainsKey(ByVal key As String) As Boolean
            Dim keyL = key.ToLower()

            SyncLock Userdata

                For Each kv In Userdata
                    If kv.Key.ToLower() = keyL Then Return True
                Next
            End SyncLock

            Return False
        End Function

        Public Function [Get](key() As Byte, ByRef value As Object) As Boolean
            Return [Get](Encoding.UTF8.GetString(key), value)
        End Function

        Public Function [Get](key As String, ByRef value As Object) As Boolean
            Dim keyL = key.ToLower()

            value = Nothing
            SyncLock Userdata
                For Each kv In Userdata
                    If kv.Key.ToLower() = keyL Then
                        value = kv
                        Exit For
                    End If
                Next
                If value Is Nothing Then Return False
                Return True
            End SyncLock

        End Function

        'VB VooDoo
#Disable Warning IDE0060 ' Remove unused parameter
        Public Function [Get](key As String, Optional onKeyNotFound As Object = Nothing, Optional IsType As Boolean = False) As Object
#Enable Warning IDE0060 ' Remove unused parameter
            Dim value As Object = Nothing

            If Not [Get](key, value) Then
                Return onKeyNotFound
            End If

            If value Is Nothing Then Return onKeyNotFound
            If Not (TypeOf value Is AccountKeyValue) Then Return value
            Return (CType(value, AccountKeyValue)).Value
        End Function

        Public Sub [Set](ByVal key As String, ByVal value As Object)
            Dim keyL = key.ToLower()

            SyncLock Userdata

                For Each kv In Userdata

                    If kv.Key.ToLower() = keyL Then
                        kv.Value = value
                        Return
                    End If
                Next
            End SyncLock

            Throw New KeyNotFoundException(key)
        End Sub

        Public Shared Function TryCreate(ByVal varUsername As String, ByVal varPasswordHash As Byte(), <Out> ByRef varAccount As Account) As CreateStatus
            varAccount = Nothing
            Dim accountJson As JsonElement = Nothing
            Settings.State.RootElement.TryGetProperty("account", accountJson)
            Dim autoAdminJson = Nothing
            accountJson.TryGetProperty("auto_admin", autoAdminJson)
            Dim disallowWordsJson = Nothing
            accountJson.TryGetProperty("disallow_words", disallowWordsJson)
            Dim maxAdjacentPunctuationJson = Nothing
            accountJson.TryGetProperty("max_adjacent_punctuation", maxAdjacentPunctuationJson)
            Dim maxLengthJson = Nothing
            accountJson.TryGetProperty("max_length", maxLengthJson)
            Dim maxPunctuationJson = Nothing
            accountJson.TryGetProperty("max_punctuation", maxPunctuationJson)
            Dim minAlphanumericJson = Nothing
            accountJson.TryGetProperty("min_alphanumeric", minAlphanumericJson)
            Dim minLengthJson = Nothing
            accountJson.TryGetProperty("min_length", minLengthJson)

            If Not disallowWordsJson.ValueKind.HasFlag(JsonValueKind.Array) Then
                Throw New NotSupportedException("Setting [account] -> [disallow_words] is not an array; check value")
            End If

            Dim autoAdmin As Boolean = False

            If autoAdminJson.ValueKind = JsonValueKind.Array Then

                For Each nameJson In autoAdminJson.EnumerateArray()
                    Dim name = nameJson.GetString()

                    If varUsername.ToLower() = name.ToLower() Then
                        autoAdmin = True
                        Exit For
                    End If
                Next
            ElseIf autoAdminJson.ValueKind = JsonValueKind.String Then
                autoAdmin = varUsername.ToLower() = autoAdminJson.GetString().ToLower()
            ElseIf autoAdminJson.ValueKind = JsonValueKind.[True] OrElse autoAdminJson.ValueKind = JsonValueKind.[False] Then
                autoAdmin = autoAdminJson.GetBoolean() AndAlso Common.AccountsDb.Count = 0
            Else
                Throw New NotSupportedException("Setting [account] -> [auto_admin] is not an array, string, or boolean; check value")
            End If

            Dim bannedWords As JsonElement = disallowWordsJson
            Dim maximumAdjacentPunctuation = maxAdjacentPunctuationJson.GetUInt32()
            Dim maximumPunctuation = maxPunctuationJson.GetUInt32()
            Dim maximumUsernameSize = maxLengthJson.GetUInt32()
            Dim minimumAlphanumericSize = minAlphanumericJson.GetUInt32()
            Dim minimumUsernameSize = minLengthJson.GetUInt32()

            SyncLock Common.AccountsProcessing

                If Common.AccountsProcessing.Contains(varUsername) Then
                    Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Account, "Still processing new account request...")
                    Return CreateStatus.LastCreateInProgress
                End If

                Common.AccountsProcessing.Add(varUsername)
            End SyncLock

            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Account, "Processing new account request...")
            If varUsername.Length < minimumUsernameSize Then Return CreateStatus.UsernameTooShort
            If varUsername.Length > maximumUsernameSize Then Return CreateStatus.UsernameShortAlphanumeric
            Dim total_alphanumeric As UInteger = 0
            Dim total_punctuation As UInteger = 0
            Dim adjacent_punctuation As UInteger = 0
            Dim last_c As Char = ChrW(0)

            For Each c In varUsername

                If Not Alphanumeric.Contains(c) AndAlso Not Punctuation.Contains(c) Then
                    Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Account, "Requested username contains invalid characters")
                    Return CreateStatus.UsernameInvalidChars
                End If

                If Alphanumeric.Contains(c) Then total_alphanumeric += 1

                If Punctuation.Contains(c) Then
                    total_punctuation += 1
                    If last_c <> Chr(0) AndAlso Punctuation.Contains(last_c) Then adjacent_punctuation += 1
                End If

                If total_punctuation > maximumPunctuation Then
                    Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Account, $"Requested username [{varUsername}] contains too many punctuation")
                    Return CreateStatus.UsernameTooManyPunctuation
                End If

                If adjacent_punctuation > maximumAdjacentPunctuation Then
                    Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Account, $"Requested username [{varUsername}] contains too many adjacent punctuation")
                    Return CreateStatus.UsernameAdjacentPunctuation
                End If

                last_c = c
            Next

            If total_alphanumeric < minimumAlphanumericSize Then
                Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Account, $"Requested username [{varUsername}] is too short or contains too few alphanumeric characters")
                Return CreateStatus.UsernameShortAlphanumeric
            End If

            'This is not here to destroy your current "BannedWords" there is an actual banned word list for
            'accounts, also titles but that one was characters only. So pulled out the curse words that
            'exist in the profanity listing, since this takes care of those words as well as the main missing 
            'profane words.
            If AtlasV.Battlenet.Protocols.Game.ProfanityFilter.ContainsProfane(varUsername) Then
                Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Account, $"Requested username [{varUsername}] contains Profanity.")
                Return CreateStatus.UsernameBannedWord
            End If

            For Each word In bannedWords.EnumerateArray()
                If varUsername.ToLower().Contains(word.GetString().ToLower()) Then
                    Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Account, $"Requested username [{varUsername}] contains a banned word or phrase")
                    Return CreateStatus.UsernameBannedWord
                End If
            Next

            SyncLock Common.AccountsDb

                If Common.AccountsDb.ContainsKey(varUsername) Then
                    Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Account, $"Requested username [{varUsername}] already exists")
                    Return CreateStatus.AccountExists
                End If

                varAccount = New Account()
                varAccount.[Set](Account.UsernameKey, varUsername)
                varAccount.[Set](Account.PasswordKey, varPasswordHash)
                varAccount.[Set](Account.FlagsKey, If(autoAdmin, (Account.Flags.Employee Or Account.Flags.Admin), Account.Flags.None))
                Common.AccountsDb.Add(varUsername, varAccount)
            End SyncLock

            SyncLock Common.AccountsProcessing
                Common.AccountsProcessing.Remove(varUsername)
            End SyncLock

            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Account, $"Created new account [{varUsername}]")
            Return CreateStatus.Success
        End Function
    End Class
End Namespace
