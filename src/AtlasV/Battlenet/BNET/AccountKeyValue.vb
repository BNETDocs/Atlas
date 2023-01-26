
Namespace AtlasV.Battlenet
    Class AccountKeyValue
        Public Enum ReadLevel
            Any
            Owner
            Internal
        End Enum

        Public Enum WriteLevel
            Any
            Owner
            Internal
            [ReadOnly]
        End Enum

        Public Property Key As String
        Public Property Readable As ReadLevel
        Public Value As Object
        Public Property Writable As WriteLevel

        Public Sub New(ByVal varKey As String,
                       ByVal varValue As Object,
                       ByVal varReadable As ReadLevel,
                       ByVal varWritable As WriteLevel)
            Key = varKey
            Value = varValue
            Readable = varReadable
            Writable = varWritable
        End Sub
    End Class
End Namespace
