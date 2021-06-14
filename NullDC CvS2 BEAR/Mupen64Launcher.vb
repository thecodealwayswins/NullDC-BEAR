﻿Imports System.IO
Imports NullDC_CvS2_BEAR.frmMain

Public Class Mupen64Launcher

    Public MupenInstance As Process = Nothing
    Public MupenServer As Process = Nothing

    Public Sub LaunchEmulator(ByVal _romname)
        Console.WriteLine("launching Mupen64Plus: " & _romname)
        Dim MupenLaunchInfo As New ProcessStartInfo
        Environment.CurrentDirectory = MainformRef.NullDCPath & "\Mupen64Plus"

        MupenLaunchInfo.FileName = MainformRef.NullDCPath & "\Mupen64Plus\mupen64plus-ui-console.exe"
        MupenLaunchInfo.Arguments = "--gfx mupen64plus-video-glide64mk2 "
        MupenLaunchInfo.Arguments += "--resolution 1280x720 "
        MupenLaunchInfo.Arguments += "--windowed "
        MupenLaunchInfo.Arguments += "--online "

        MupenLaunchInfo.Arguments += """" & MainformRef.NullDCPath & "\" & MainformRef.GamesList(_romname)(1) & """"

        MupenInstance = Process.Start(MupenLaunchInfo)
        MupenInstance.EnableRaisingEvents = True

        Environment.CurrentDirectory = MainformRef.NullDCPath

        AddHandler MupenInstance.Exited, AddressOf MupenExited

    End Sub

    Private Sub MupenExited()

        Rx.EEPROM = ""

        If Not MainformRef.IsClosing Then
            Dim INVOKATION As EndSession_delegate = AddressOf MainformRef.EndSession
            MainformRef.Invoke(INVOKATION, {"Window Closed", Nothing})
        End If

        MupenInstance = Nothing

    End Sub

End Class
