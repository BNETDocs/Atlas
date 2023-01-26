Imports System.Net
Imports System.Net.Sockets

Namespace AtlasV.Battlenet.Protocols
    Interface IListener
        Property LocalEndPoint() As IPEndPoint
        ReadOnly Property IsListening As Boolean
        Property Socket As Socket
        Sub Close()
        Sub SetLocalEndPoint(ByVal varEndPoint As IPEndPoint)
        Sub Start()
        Sub [Stop]()
    End Interface
End Namespace
