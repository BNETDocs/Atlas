Imports System

Namespace AtlasV.Battlenet.Protocols.Game
    Structure LocaleInfo
        Public SystemLocaleId As UInt32
        Public UserLocaleId As UInt32
        Public UserLanguageId As UInt32
        Public LanguageCode As UInt32
        Public LanguageNameAbbreviated As String
        Public CountryCode As String
        Public CountryNameAbbreviated As String
        Public CountryName As String
    End Structure
End Namespace