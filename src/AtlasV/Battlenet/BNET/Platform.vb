Imports System

Namespace AtlasV.Battlenet
    Public Class Platform
        Public Enum PlatformCode As UInt32
            None = 0UI
            MacOSClassic = &H504D4143UI
            MacOSX = &H584D4143UI
            Windows = &H49583836UI
        End Enum

#Disable Warning IDE0060 ' Remove unused parameter
        Public Shared Function PlatformName(ByVal code As PlatformCode, ByVal Optional extended As Boolean = True) As String
#Enable Warning IDE0060 ' Remove unused parameter
            Select Case code
                Case PlatformCode.None : Return "None"
                Case PlatformCode.MacOSClassic : Return "Mac OS Classic"
                Case PlatformCode.MacOSX : Return "Mac OS X"
                Case PlatformCode.Windows : Return "Windows"
                Case Else : Return "Unknown"
            End Select
        End Function
    End Class
End Namespace