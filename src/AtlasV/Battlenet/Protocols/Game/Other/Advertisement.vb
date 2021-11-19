Imports System
Imports System.Collections.Generic
Imports System.IO

Namespace AtlasV.Battlenet.Protocols.Game
    Class Advertisement
        Public Property Filename As String
        Public Property Filetime As DateTime
        Public Property Url As String
        Public Property Products As List(Of Product.ProductCode)
        Public Property Locales As List(Of UInteger)

        Public Sub New(ByVal varFilename As String,
                       ByVal varUrl As String,
                       ByVal Optional varProducts As List(Of Product.ProductCode) = Nothing,
                       ByVal Optional varLocales As List(Of UInteger) = Nothing)
            Filename = varFilename
            Filetime = File.GetLastWriteTime(varFilename)
            Url = varUrl
            Products = varProducts
            Locales = varLocales
        End Sub
    End Class
End Namespace
