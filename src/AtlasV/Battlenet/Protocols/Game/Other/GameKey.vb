Imports AtlasV.Battlenet.Exceptions
Imports AtlasV.Daemon
Imports System
Imports System.IO
Imports System.Linq
Imports System.Text

Namespace AtlasV.Battlenet.Protocols.Game
    Class GameKey
        Public Enum ProductValues As UInt32
            Starcraft_A = &H1
            Starcraft_B = &H2
            WarcraftII = &H4
            DiabloIIBeta = &H5
            StarcraftIIBeta = &H5
            WorldOfWarcraftWrathOfTheLichKingAlpha = &H5
            DiabloII_A = &H6
            DiabloII_B = &H7
            DiabloIIStressTest = &H9
            DiabloIILordOfDestruction_A = &HA
            DiabloIILordOfDestruction_Beta = &HB
            DiabloIILordOfDestruction_B = &HC
            WarcraftIIIBeta = &HD
            WarcraftIIIReignOfChaos_A = &HE
            WarcraftIIIReignOfChaos_B = &HF
            WarcraftIIIFrozenThroneBeta = &H11
            WarcraftIIIFrozenThrone_A = &H12
            WarcraftIIIFrozenThrone_B = &H13
            WorldOfWarcraftBurningCrusade = &H15
            WorldOfWarcraft14DayTrial = &H16
            Starcraft_DigitalDownload = &H17
            DiabloII_DigitalDownload = &H18
            DiabloIILordOfDestruction_DigitalDownload = &H19
            WorldOfWarcraftWrathOfTheLichKing = &H1A
            StarcraftII = &H1C
            DiabloIII = &H1D
            HeroesOfTheStorm = &H24
        End Enum

        Public Property PrivateValue As Byte()
        Public ProductValue As UInt32
        Public PublicValue As UInt32

        Public Sub New(ByVal varKeyLength As UInt32,
                       ByVal varProductValue As UInt32,
                       ByVal varPublicValue As UInt32,
                       ByVal varHashedKeyData As Byte())
            If Not (varKeyLength = 13 OrElse varKeyLength = 16 OrElse varKeyLength = 26) Then Throw New GameProtocolViolationException(Nothing, "Invalid game key length")
            If Not IsValidProductValue(CType(ProductValue, ProductValues)) Then Throw New GameProtocolViolationException(Nothing, "Invalid game key product value")
            SetProductValue(varProductValue)
            SetPublicValue(varPublicValue)
            SetPrivateValue(varHashedKeyData)
        End Sub

        Public Sub New(ByVal varProductValue As UInt32, ByVal varPublicValue As UInt32, ByVal varPrivateValue As Byte())
            SetProductValue(varProductValue)
            SetPublicValue(varPublicValue)
            SetPrivateValue(varPrivateValue)
        End Sub

        Public Sub New(ByVal varKeyString As String)
            Dim m_gameKey = New MBNCSUtil.CdKey(varKeyString)

            If m_gameKey Is Nothing OrElse Not m_gameKey.IsValid Then
                Throw New GameProtocolViolationException(Nothing, "Cannot parse invalid game key")
            End If

            SetPrivateValue(m_gameKey.GetValue2())
            SetProductValue(CUInt(m_gameKey.Product))
            SetPublicValue(CUInt(m_gameKey.Value1))
        End Sub

        Public Function IsValidProductValue() As Boolean
            Return GameKey.IsValidProductValue(CType(ProductValue, ProductValues))
        End Function

        Public Shared Function IsValidProductValue(ByVal varProductValue As ProductValues) As Boolean
            Select Case varProductValue
                Case ProductValues.DiabloIIBeta,
                     ProductValues.DiabloIILordOfDestruction_A,
                     ProductValues.DiabloIILordOfDestruction_B,
                     ProductValues.DiabloIILordOfDestruction_Beta,
                     ProductValues.DiabloIILordOfDestruction_DigitalDownload,
                     ProductValues.DiabloIIStressTest,
                     ProductValues.DiabloII_A,
                     ProductValues.DiabloII_B,
                     ProductValues.DiabloII_DigitalDownload,
                     ProductValues.Starcraft_A,
                     ProductValues.Starcraft_B,
                     ProductValues.Starcraft_DigitalDownload,
                     ProductValues.WarcraftII,
                     ProductValues.WarcraftIIIBeta,
                     ProductValues.WarcraftIIIFrozenThroneBeta,
                     ProductValues.WarcraftIIIFrozenThrone_A,
                     ProductValues.WarcraftIIIFrozenThrone_B,
                     ProductValues.WarcraftIIIReignOfChaos_A,
                     ProductValues.WarcraftIIIReignOfChaos_B
                    Return True
                Case Else
                    Return False
            End Select
        End Function

        Public Shared Function RequiredKeyCount(ByVal varCode As Product.ProductCode) As UInteger
            Dim buf = New Byte(3) {}

            Using m = New MemoryStream(buf)
                Using w = New BinaryWriter(m)
                    w.Write(CUInt(varCode))
                End Using
            End Using

            Dim productStr = Daemon.Common.ReverseString(Encoding.UTF8.GetString(buf))
            Dim battlenetJson = Nothing, emulationJson = Nothing, requiredGameKeyCountJson = Nothing, productJson = Nothing

            Try
                Settings.State.RootElement.TryGetProperty("battlenet", battlenetJson)
                battlenetJson.TryGetProperty("emulation", emulationJson)
                emulationJson.TryGetProperty("required_game_key_count", requiredGameKeyCountJson)
                requiredGameKeyCountJson.TryGetProperty(productStr, productJson)
                Return productJson.GetUInt32()
            Catch ex As Exception
                If Not (TypeOf ex Is ArgumentNullException OrElse TypeOf ex Is InvalidOperationException) Then Throw
                Return 0
            End Try
        End Function

        Public Sub SetPrivateValue(ByVal varPrivateValue As Byte())
            If Not (PrivateValue.Length = 4 OrElse PrivateValue.Length = 20) Then Throw New GameProtocolViolationException(Nothing, "Invalid game key private value")
            PrivateValue = varPrivateValue
        End Sub

        Public Sub SetProductValue(ByVal varProductValue As UInt32)
            ProductValue = varProductValue
        End Sub

        Public Sub SetPublicValue(ByVal varPublicValue As UInt32)
            PublicValue = varPublicValue
        End Sub
    End Class

End Namespace