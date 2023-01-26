Imports AtlasV.Daemon
Imports System
Imports System.IO

Namespace AtlasV.Battlenet.Protocols.BNFTP
    Class File
        Implements IDisposable

#Disable Warning CA1822 ' Mark members as static
        Public ReadOnly Property BNFTPPath As String
#Enable Warning CA1822 ' Mark members as static
            Get
                Return Settings.GetString(New String() {"bnftp", "root"}, Nothing)
            End Get
        End Property

        Public ReadOnly Property Exists As Boolean
            Get
                Return GetFileInfo().Exists
            End Get
        End Property

        Public ReadOnly Property IsDirectory As Boolean
            Get
                Return GetFileInfo().Attributes.HasFlag(FileAttributes.Directory)
            End Get
        End Property

        Public Property Name As String

        Public ReadOnly Property LastAccessTime As DateTime
            Get
                Return System.IO.File.GetLastAccessTime(System.IO.Path.Combine(BNFTPPath, Name))
            End Get
        End Property

        Public ReadOnly Property LastAccessTimeUtc As DateTime
            Get
                Return System.IO.File.GetLastAccessTimeUtc(System.IO.Path.Combine(BNFTPPath, Name))
            End Get
        End Property

        Public ReadOnly Property Length As Long
            Get
                Return GetFileInfo().Length
            End Get
        End Property

        Public ReadOnly Property Path As String
            Get
                Return System.IO.Path.GetFullPath(System.IO.Path.Combine(BNFTPPath, Name))
            End Get
        End Property

        Public Property StreamReader As StreamReader = Nothing

        Public Sub New(ByVal filename As String)
            Name = filename
        End Sub

        Public Sub Close()
            CloseStream()
        End Sub

        Public Sub CloseStream()
            If StreamReader IsNot Nothing Then
                StreamReader.Close()
            End If
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            Close()
        End Sub

        Public Function GetFileInfo(ByVal Optional ignoreLimits As Boolean = False) As FileInfo
            Dim rootStr = System.IO.Path.GetFullPath(BNFTPPath)
            Dim pathStr = System.IO.Path.GetFullPath(System.IO.Path.Combine(rootStr, Name))

            If Not ignoreLimits AndAlso (pathStr.Length < rootStr.Length OrElse pathStr.Substring(0, rootStr.Length) <> rootStr) Then
                Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.BNFTP, $"Error retrieving file info; path would leave BNFTP root directory")
                Return Nothing
            End If

            Dim fileinfo As FileInfo

            Try
                fileinfo = New FileInfo(System.IO.Path.Combine(BNFTPPath, Name))
            Catch ex As Exception
                If Not (TypeOf ex Is UnauthorizedAccessException OrElse TypeOf ex Is PathTooLongException OrElse TypeOf ex Is NotSupportedException) Then Throw
                Return Nothing
            End Try

            If fileinfo Is Nothing Then
                Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.BNFTP, $"Error retrieving file info for [{Name}]; null FileInfo object")
                Return Nothing
            End If

            If Not ignoreLimits AndAlso Not fileinfo.Exists Then
                Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.BNFTP, $"Error retrieving file info for [{Name}]; file not found")
                Return Nothing
            End If

            If Not ignoreLimits AndAlso fileinfo.Attributes.HasFlag(FileAttributes.Directory) Then
                Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.BNFTP, $"Error retrieving file info for [{Name}]; path pointed to a directory")
                Return Nothing
            End If

            Return fileinfo
        End Function

        Public Function OpenStream() As Boolean
            If StreamReader IsNot Nothing Then
                StreamReader.Close()
            End If

            StreamReader = Nothing
            Dim fileinfo = GetFileInfo()
            If fileinfo Is Nothing Then Return False
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.BNFTP, $"Opening read stream for [{fileinfo.FullName}]...")
            StreamReader = New StreamReader(fileinfo.FullName)
            Return True
        End Function
    End Class
End Namespace