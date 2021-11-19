Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Text.Json
Imports System.Threading
Imports System.Threading.Tasks

Namespace AtlasV.Daemon

    Public Class Settings

        Public Const CONFIG_FILE As String = "atlasv.json"
        Public Const DocumentVersionSupported As UInteger = 0

        Private Shared mState As JsonDocument
        Public Shared Property State As JsonDocument
            Get
                Return mState
            End Get
            Private Set(value As JsonDocument)
                mState = value
            End Set
        End Property
        Private Shared mPath As String
        Public Shared Property Path As String
            Get
                Return mPath
            End Get
            Private Set(value As String)
                mPath = value
            End Set
        End Property

        Public Shared Function CanRead() As Boolean
            Try
                If File.Exists(Path) = False Then
                    Throw New FileLoadException("File dosent exist.", Path)
                End If
                Dim fInfo As New FileInfo(Path)

                If fInfo Is Nothing Then
                    Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"Configuration file [{Path}] returned null FileInfo object; check filesystem")
                    Return False
                End If

                If Not (fInfo.Exists) Then 'this is taken care of above L#29
                    Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"Configuration file [{Path}] does not exist; check file")
                    Return False
                End If

                If fInfo.Attributes.HasFlag(FileAttributes.Directory) Then
                    Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"Configuration file [{Path}] points to a directory; check path string")
                    Return False
                End If

                If fInfo.Length = 0 Then
                    Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"Configuration file [{Path}] is empty; check file")
                    Return False
                End If
            Catch ex As ArgumentNullException
                Return False
            Catch ex As UnauthorizedAccessException
                Return False
            Catch ex As PathTooLongException
                Return False
            Catch ex As Exception
                Throw New Exception(ex.Message, ex.InnerException)
                Return False
            End Try

            Return True
        End Function

        Public Shared Function GetArray(keyPath() As String) As JsonElement.ArrayEnumerator
            Try
                Dim json = State.RootElement
                For Each key In keyPath
                    json.TryGetProperty(key, json)
                Next
                Return json.EnumerateArray()
            Catch ex As ArgumentNullException
                Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"Setting [{String.Join("] -> [", keyPath)}] is not an array; check value")
                Return New JsonElement.ArrayEnumerator()
            Catch ex As InvalidOperationException
                Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"Setting [{String.Join("] -> [", keyPath)}] is not an array; check value")
                Return New JsonElement.ArrayEnumerator()
            Catch ex As Exception
                Throw New Exception(ex.Message, ex.InnerException)
                Return New JsonElement.ArrayEnumerator()
            End Try
        End Function

        Public Shared Function GetBoolean(keyPath() As String, defaultValue As Boolean) As Boolean
            Try
                Dim json = State.RootElement
                For Each key In keyPath
                    json.TryGetProperty(key, json)
                Next
                Return json.GetBoolean()
            Catch ex As ArgumentNullException
                Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"Setting [{String.Join("] -> [", keyPath)}] is not an boolean; check value")
                Return defaultValue
            Catch ex As InvalidOperationException
                Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"Setting [{String.Join("] -> [", keyPath)}] is not an boolean; check value")
                Return defaultValue
            Catch ex As Exception
                Throw New Exception(ex.Message, ex.InnerException)
                Return defaultValue
            End Try
        End Function

        Public Shared Function GetByte(keyPath() As String, defaultValue As Byte) As Byte
            Try
                Dim json = State.RootElement
                For Each key In keyPath
                    json.TryGetProperty(key, json)
                Next
                Return json.GetByte()
            Catch ex As ArgumentNullException
                Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"Setting [{String.Join("] -> [", keyPath)}] is not an byte; check value")
                Return defaultValue
            Catch ex As InvalidOperationException
                Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"Setting [{String.Join("] -> [", keyPath)}] is not an byte; check value")
                Return defaultValue
            Catch ex As Exception
                Throw New Exception(ex.Message, ex.InnerException)
                Return defaultValue
            End Try
        End Function

        Public Shared Function GetInt16(keyPath() As String, defaultValue As Int16) As Int16
            Try
                Dim json = State.RootElement
                For Each key In keyPath
                    json.TryGetProperty(key, json)
                Next
                Return json.GetInt16()
            Catch ex As ArgumentNullException
                Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"Setting [{String.Join("] -> [", keyPath)}] is not an Int16; check value")
                Return defaultValue
            Catch ex As InvalidOperationException
                Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"Setting [{String.Join("] -> [", keyPath)}] is not an Int16; check value")
                Return defaultValue
            Catch ex As Exception
                Throw New Exception(ex.Message, ex.InnerException)
                Return defaultValue
            End Try
        End Function

        Public Shared Function GetInt32(keyPath() As String, defaultValue As Int32) As Int32
            Try
                Dim json = State.RootElement
                For Each key In keyPath
                    json.TryGetProperty(key, json)
                Next
                Return json.GetInt32()
            Catch ex As ArgumentNullException
                Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"Setting [{String.Join("] -> [", keyPath)}] is not an Int32; check value")
                Return defaultValue
            Catch ex As InvalidOperationException
                Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"Setting [{String.Join("] -> [", keyPath)}] is not an Int32; check value")
                Return defaultValue
            Catch ex As Exception
                Throw New Exception(ex.Message, ex.InnerException)
                Return defaultValue
            End Try
        End Function

        Public Shared Function GetInt64(keyPath() As String, defaultValue As Int64) As Int64
            Try
                Dim json = State.RootElement
                For Each key In keyPath
                    json.TryGetProperty(key, json)
                Next
                Return json.GetInt64()
            Catch ex As ArgumentNullException
                Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"Setting [{String.Join("] -> [", keyPath)}] is not an Int64; check value")
                Return defaultValue
            Catch ex As InvalidOperationException
                Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"Setting [{String.Join("] -> [", keyPath)}] is not an Int64; check value")
                Return defaultValue
            Catch ex As Exception
                Throw New Exception(ex.Message, ex.InnerException)
                Return defaultValue
            End Try
        End Function

        Public Shared Function GetSByte(keyPath() As String, defaultValue As SByte) As SByte
            Try
                Dim json = State.RootElement
                For Each key In keyPath
                    json.TryGetProperty(key, json)
                Next
                Return json.GetSByte()
            Catch ex As ArgumentNullException
                Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"Setting [{String.Join("] -> [", keyPath)}] is not an SByte; check value")
                Return defaultValue
            Catch ex As InvalidOperationException
                Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"Setting [{String.Join("] -> [", keyPath)}] is not an SByte; check value")
                Return defaultValue
            Catch ex As Exception
                Throw New Exception(ex.Message, ex.InnerException)
                Return defaultValue
            End Try
        End Function

        Public Shared Function GetString(keyPath() As String, defaultValue As String) As String
            Try
                Dim json = State.RootElement
                For Each key In keyPath
                    json.TryGetProperty(key, json)
                Next
                Return json.GetString()
            Catch ex As ArgumentNullException
                Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"Setting [{String.Join("] -> [", keyPath)}] is not an String; check value")
                Return defaultValue
            Catch ex As InvalidOperationException
                Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"Setting [{String.Join("] -> [", keyPath)}] is not an String; check value")
                Return defaultValue
            Catch ex As Exception
                Throw New Exception(ex.Message, ex.InnerException)
                Return defaultValue
            End Try
        End Function

        Public Shared Function GetUInt16(keyPath() As String, defaultValue As UInt16) As UInt16
            Try
                Dim json = State.RootElement
                For Each key In keyPath
                    json.TryGetProperty(key, json)
                Next
                Return json.GetUInt16()
            Catch ex As ArgumentNullException
                Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"Setting [{String.Join("] -> [", keyPath)}] is not an UInt16; check value")
                Return defaultValue
            Catch ex As InvalidOperationException
                Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"Setting [{String.Join("] -> [", keyPath)}] is not an UInt16; check value")
                Return defaultValue
            Catch ex As Exception
                Throw New Exception(ex.Message, ex.InnerException)
                Return defaultValue
            End Try
        End Function

        Public Shared Function GetUInt32(keyPath() As String, defaultValue As UInt32) As UInt32
            Try
                Dim json = State.RootElement
                For Each key In keyPath
                    json.TryGetProperty(key, json)
                Next
                Return json.GetUInt32()
            Catch ex As ArgumentNullException
                Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"Setting [{String.Join("] -> [", keyPath)}] is not an UInt32; check value")
                Return defaultValue
            Catch ex As InvalidOperationException
                Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"Setting [{String.Join("] -> [", keyPath)}] is not an UInt32; check value")
                Return defaultValue
            Catch ex As Exception
                Throw New Exception(ex.Message, ex.InnerException)
                Return defaultValue
            End Try
        End Function

        Public Shared Function GetUInt64(keyPath() As String, defaultValue As UInt64) As UInt64
            Try
                Dim json = State.RootElement
                For Each key In keyPath
                    json.TryGetProperty(key, json)
                Next
                Return json.GetUInt64()
            Catch ex As ArgumentNullException
                Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"Setting [{String.Join("] -> [", keyPath)}] is not an UInt64; check value")
                Return defaultValue
            Catch ex As InvalidOperationException
                Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"Setting [{String.Join("] -> [", keyPath)}] is not an UInt64; check value")
                Return defaultValue
            Catch ex As Exception
                Throw New Exception(ex.Message, ex.InnerException)
                Return defaultValue
            End Try
        End Function

        Public Shared Sub Initialize()
            If (String.IsNullOrEmpty(Path)) Then
                SetPathToDefault()
            End If

            Reset()
            Load()
        End Sub

        Public Shared Sub SetPathToDefault()
            Path = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, CONFIG_FILE))
        End Sub

        Public Shared Sub Reset()
            Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Config, "Resetting to default configuration")
            State = JsonDocument.Parse("{}")
        End Sub

        Public Shared Sub Load()
            Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Config, $"Loading configuration from [{CONFIG_FILE}]") 'Path
            If CanRead() = False Then
                Throw New InvalidOperationException("Load()->CanRead()==false")
            End If
            Try
                Dim jsonOpts = New JsonDocumentOptions With
                {
                    .AllowTrailingCommas = True,
                    .CommentHandling = JsonCommentHandling.Skip,
                    .MaxDepth = 10
                }

                Dim json As String
                Using r As New StreamReader(Path)
                    json = r.ReadToEnd()
                End Using
                Dim fileState = JsonDocument.Parse(json, jsonOpts)

                Dim documentVersionJson As JsonElement
                If fileState.RootElement.TryGetProperty("document_version", documentVersionJson) = False Then
                    Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"Document version missing, refusing to continue")
                    Return
                End If
                Dim documentVersionInt As UInt32
                If documentVersionJson.TryGetUInt32(documentVersionInt) = False Then
                    Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"Document version is not of a uint32 type, refusing to continue")
                    Return
                End If
                If documentVersionInt <> DocumentVersionSupported Then
                    Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"Document version {documentVersionInt} is different than supported version {DocumentVersionSupported}, refusing to continue")
                    Return
                End If
                State = fileState
                Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Config, "Loaded configuration")

            Catch ex As FileNotFoundException
                Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"File not found [{Path}]")
            Catch ex As InvalidOperationException
                Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"{ex.GetType().Name} occurred while parsing [{Path}], refusing to continue")
            Catch ex As Exception
                Throw New Exception(ex.Message, ex.InnerException)
            End Try
        End Sub

        Public Shared Sub Save()
            Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Config, $"Saving configuration to [{Path}]")
            Dim jsonUtf8Bytes() As Byte
            Try
                Dim jsonOpts = New JsonSerializerOptions With
                {
                    .MaxDepth = 10,
                    .WriteIndented = True
                }
                jsonUtf8Bytes = JsonSerializer.SerializeToUtf8Bytes(State, jsonOpts)
                Using varStream = New StreamWriter(Path)
                    varStream.Write(jsonUtf8Bytes)
                End Using
                Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Config, "Saved configuration")
            Catch ex As IOException
                Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, "Failed to save configuration due to IOException")
            End Try
        End Sub

        Public Shared Sub SetPath(varFile As String)
            Path = varFile
        End Sub

    End Class

End Namespace