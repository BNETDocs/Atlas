Imports AtlasV.Daemon
Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Runtime.CompilerServices
Imports System.Text
Imports System.Text.Json

Namespace AtlasV.Battlenet.Protocols.Game
    Public Class ProfanityFilter
        'J S O N  F I L E  C O N S T A N T S
        Private Const json_title As String = "profanity_filter"
        Private Const json_key As String = "key"
        Private Const json_value As String = "value"

        'Destination array was not long enough. Check the destination index, length, and the array's lower bounds.
        '   Error that occures with array.copy if the location being overwritten is over the length range of the <out>array
        Private Const KEY_VALUE_LENGTH_ARGUMENTEXCEPTION As String = "Check your JSON file and ensure your key length and value length match."

        Private Class ProfanityFilterKeySet
            Public Property Key As String
            Public Property Value As Byte()
            Public Sub New(varKey As String, varValue() As Byte)
                Key = varKey
                Value = varValue
            End Sub
        End Class
        Private Shared Property ChatFilterListing As List(Of ProfanityFilterKeySet)

        Private Shared mLockObj As Object = New Object
        Public Shared ReadOnly Property LockObject As Object
            Get
                Return mLockObj
            End Get
        End Property
        Private Shared Property ActiveFilterList As Boolean

        Public Shared Sub Initialize()
            ActiveFilterList = False
            If ChatFilterListing Is Nothing OrElse ChatFilterListing.Count <> 0 Then
                ChatFilterListing = New List(Of ProfanityFilterKeySet)()
                Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Config, "Initializing Profanity Filter")
            Else
                ChatFilterListing.Clear()
                Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Config, "ReInitializing Profanity Filter")
            End If

            Dim locProfanityFilterJson As JsonElement = Nothing, locKeyJson As JsonElement = Nothing, locValueJson As JsonElement = Nothing
            Settings.State.RootElement.TryGetProperty(json_title, locProfanityFilterJson)
            Dim locKey, locValue As String

            If locProfanityFilterJson.ValueKind = JsonValueKind.Undefined Then
                Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Config, $"No {json_title} key found, defaulting to no profanity filter.")
                Return
            End If
            'Make sure the title exists, and is of type array
            If locProfanityFilterJson.ValueKind <> JsonValueKind.Undefined AndAlso locProfanityFilterJson.ValueKind = JsonValueKind.Array Then
                For Each locProfanity In locProfanityFilterJson.EnumerateArray()
                    locProfanity.TryGetProperty(json_key, locKeyJson)
                    locProfanity.TryGetProperty(json_value, locValueJson)

                    If locKeyJson.ValueKind <> JsonValueKind.String OrElse locValueJson.ValueKind <> JsonValueKind.String Then Continue For

                    'since were searching in lowercase, set the values to lower case
                    locKey = locKeyJson.GetString().ToLower()
                    locValue = locValueJson.GetString().ToLower()

                    If (Not String.IsNullOrEmpty(locKey)) AndAlso
                            (Not String.IsNullOrEmpty(locValue)) AndAlso
                            locKey.Length = locValue.Length Then

                        Dim locProfane = New ProfanityFilterKeySet(locKey, Encoding.UTF8.GetBytes(locValue))
                        SyncLock LockObject
                            ChatFilterListing.Add(locProfane)
                        End SyncLock

                    End If
                Next
            End If
            If ChatFilterListing.Count > 0 Then
                ActiveFilterList = True
            End If
            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Config, $"Initialized {ChatFilterListing.Count} Profanity Filter Keys.")
        End Sub

        Public Shared Function FilterMessage(varByteArray() As Byte) As Byte()
            Try
                If Not ActiveFilterList Then Return varByteArray
                If ChatFilterListing Is Nothing Then Return varByteArray
                Dim finalArray() As Byte = varByteArray
                Dim lowerString As String = Encoding.UTF8.GetString(varByteArray).ToLower()

                SyncLock LockObject
                    Dim locIndex As Integer = -1
                    For Each SetOfKeys In ChatFilterListing
                        locIndex = lowerString.IndexOf(SetOfKeys.Key)
                        If locIndex >= 0 Then
                            Array.Copy(SetOfKeys.Value, 0, finalArray, locIndex, SetOfKeys.Value.Length)
                        End If
                    Next
                End SyncLock

                Return finalArray
            Catch ex As ArgumentException
                Throw New ArgumentException(KEY_VALUE_LENGTH_ARGUMENTEXCEPTION)
            End Try
        End Function
        Public Shared Function FilterMessage(varString As String) As Byte()
            Return FilterMessage(Encoding.UTF8.GetBytes(varString))
        End Function

        ''' <summary>
        ''' Just a manual disposal of our static list nothing big.
        ''' </summary>
        Public Shared Sub Dispose()
            If ChatFilterListing IsNot Nothing Then
                ChatFilterListing.Clear()
                ChatFilterListing = Nothing
            End If
        End Sub

    End Class
End Namespace
