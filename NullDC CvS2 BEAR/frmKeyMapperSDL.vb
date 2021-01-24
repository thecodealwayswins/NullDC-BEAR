﻿Imports System.Runtime.InteropServices
Imports SDL2
Imports SDL2.SDL
Imports System.IO

Public Class frmKeyMapperSDL

    ' From the Configs
    Dim Joystick(2) As Int16
    Dim Deadzone(2) As Int16
    Dim Peripheral(2) As Int16
    Dim MednafenControllerID(16) As String ' Support up to 16 Controllers at once

    Public Joy As IntPtr
    Dim AvailableControllersList As New DataTable

    Dim _InputThread As Threading.Thread

    Dim Currently_Binding As Button

    Dim ButtonsDown As New Dictionary(Of String, Boolean)

    <DllImport("user32.dll", SetLastError:=True, CharSet:=CharSet.Auto)>
    Private Shared Function PostMessage(ByVal hWnd As IntPtr, ByVal Msg As UInteger, ByVal wParam As IntPtr, ByVal lParam As IntPtr) As Boolean
    End Function

    Private Declare Function GetActiveWindow Lib "user32" Alias "GetActiveWindow" () As IntPtr

    Private Sub frmKeyMapperSDL_VisibilityChange(sender As Object, e As EventArgs) Handles MyBase.VisibleChanged
        SDL.SDL_SetHint(SDL_HINT_XINPUT_ENABLED, "1") ' the default is 1, but just in case...

        If Me.Visible Then

            Me.CenterToParent()
            Me.Icon = My.Resources.NewNullDCBearIcon

            If SDL_WasInit(SDL_INIT_JOYSTICK) = 0 Then
                SDL_Init(SDL_INIT_JOYSTICK)
                Console.WriteLine("SDL_INIT JOYSTICK")
            End If

            If SDL_WasInit(SDL_INIT_GAMECONTROLLER) = 0 Then
                SDL_Init(SDL_INIT_GAMECONTROLLER)
                Console.WriteLine("SDL_INIT GAME CONTROLLER")
            End If

            DoInitialSetupShit()
            LoadSettings()

            _InputThread = New Threading.Thread(AddressOf InputThread)
            _InputThread.IsBackground = True
            _InputThread.Start()

            AddHandler cbSDL.SelectedIndexChanged, AddressOf SDLVersionChanged
            AddHandler DeadzoneTB.MouseCaptureChanged, AddressOf DeadzoneTB_MouseCaptureChanged
            AddHandler DeadzoneTB.ValueChanged, Sub() Deadzonetext.Text = "Deadzone: " & DeadzoneTB.Value
            AddHandler PeripheralCB.SelectedIndexChanged, AddressOf PeripheralCB_SelectedIndexChanged
            AddHandler PlayerTab.SelectedIndexChanged, AddressOf PlayerTab_SelectedIndexChanged
            AddHandler ControllersTab.SelectedIndexChanged, Sub() ActiveControl = Nothing


            If MainformRef.IsNullDCRunning Then
                PeripheralCB.Enabled = False
                PeriWarning.Visible = True
                cbSDL.Enabled = False
            Else
                PeripheralCB.Enabled = True
                PeriWarning.Visible = False
                cbSDL.Enabled = True
            End If

        End If


    End Sub

    Private Sub SDLVersionChanged(sender As ComboBox, e As EventArgs)
        Me.Invoke(Sub()
                      If Not cbSDL.SelectedItem = MainformRef.ConfigFile.SDLVersion Then
                          MainformRef.ConfigFile.SDLVersion = "+" & sender.SelectedItem
                          MainformRef.ConfigFile.SaveFile(False)
                          If MsgBox("Restart Required") = MsgBoxResult.Ok Then End
                      End If
                  End Sub)
    End Sub

    Private Sub DoInitialSetupShit()

        AvailableControllersList.Columns.Add("ID")
        AvailableControllersList.Columns.Add("Name")

        'AvailableControllersList.Rows.Add({-1, "Keyboard Only"})

        ControllerCB.ValueMember = "ID"
        ControllerCB.DisplayMember = "Name"

        ' Flush Initial Connection Events
        SDL_FlushEvents(SDL_EventType.SDL_CONTROLLERDEVICEADDED, SDL_EventType.SDL_CONTROLLERDEVICEREMOVED)
        UpdateControllersList()

    End Sub

    Private Sub GenerateDefaults()

        Joystick(0) = 0
        Joystick(1) = -1
        Deadzone(0) = 15
        Deadzone(1) = 15
        DeadzoneTB.Value = 15
        Peripheral(0) = 0
        Peripheral(1) = 0

        I_BTN5_KEY.KC = {"a4+", "k79"}
        I_BTN4_KEY.KC = {"b1", "k73"}
        I_BTN3_KEY.KC = {"b0", "k85"}
        I_BTN2_KEY.KC = {"a5+", "k48"}
        I_BTN1_KEY.KC = {"b3", "k57"}
        I_BTN0_KEY.KC = {"b2", "k56"}
        I_TEST_KEY_1.KC = {"k116", "k116"}
        I_SERVICE_KEY_1.KC = {"k115", "k115"}
        I_COIN_KEY.KC = {"b4", "k49"}
        I_START_KEY.KC = {"b6", "k53"}
        I_RIGHT_KEY.KC = {"a0+", "k68"}
        I_LEFT_KEY.KC = {"a0-", "k65"}
        I_DOWN_KEY.KC = {"a1+", "k83"}
        I_UP_KEY.KC = {"a1-", "k87"}
        CONT_A.KC = {"b0", "k85"}
        CONT_B.KC = {"b1", "k73"}
        CONT_Y.KC = {"b3", "k57"}
        CONT_X.KC = {"b2", "k56"}
        CONT_START.KC = {"b6", "k53"}
        CONT_RSLIDER.KC = {"a5+", "k79"}
        CONT_LSLIDER.KC = {"a4+", "k48"}
        CONT_ANALOG_DOWN.KC = {"a1+", "k83"}
        CONT_ANALOG_RIGHT.KC = {"a0+", "k68"}
        CONT_ANALOG_UP.KC = {"a1-", "k87"}
        CONT_ANALOG_LEFT.KC = {"a0-", "k65"}
        CONT_DPAD_DOWN.KC = {"b12", "k71"}
        CONT_DPAD_RIGHT.KC = {"b14", "k72"}
        CONT_DPAD_UP.KC = {"b11", "k84"}
        CONT_DPAD_LEFT.KC = {"b13", "k70"}
        STICK_C.KC = {"a5+", "k79"}
        STICK_B.KC = {"b1", "k73"}
        STICK_A.KC = {"b0", "k85"}
        STICK_Z.KC = {"a4+", "k48"}
        STICK_Y.KC = {"b3", "k57"}
        STICK_X.KC = {"b2", "k56"}
        STICK_START.KC = {"b6", "k53"}
        STICK_DPAD_RIGHT.KC = {"a0+", "k68"}
        STICK_DPAD_LEFT.KC = {"a0-", "k65"}
        STICK_DPAD_DOWN.KC = {"a1+", "k83"}
        STICK_DPAD_UP.KC = {"a1-", "k87"}

        ' Mednafen



        SaveSettings()

    End Sub

    Private Sub UpdateButtonLabels()
        For Each _TabPages As TabPage In ControllersTab.TabPages
            For Each _TabPageControl As Control In _TabPages.Controls
                If TypeOf _TabPageControl Is TabControl Then
                    Dim _innerTabControl As TabControl = _TabPageControl
                    For Each _innerTabPages As TabPage In _innerTabControl.TabPages
                        For Each _innerTabPageControl As Control In _innerTabPages.Controls
                            If TypeOf _innerTabPageControl Is keybindButton Then
                                Dim _innerTabPageControl2 As keybindButton = _innerTabPageControl
                                _innerTabPageControl2.UpdateTextFromPortID(PlayerTab.SelectedIndex)
                            End If
                        Next
                    Next

                End If
            Next
        Next

    End Sub

    Private Sub SaveSettings()

        Dim lines(128) As String
        ' BPortA_I_
        ' BPortA_CONT_
        ' get GUIDs
        Dim DeviceGUIDasString1(40) As Byte
        Dim DeviceGUIDasString2(40) As Byte

        SDL_JoystickGetGUIDString(SDL_JoystickGetDeviceGUID(Joystick(0)), DeviceGUIDasString1, 40)
        SDL_JoystickGetGUIDString(SDL_JoystickGetDeviceGUID(Joystick(1)), DeviceGUIDasString2, 40)

        Dim Joy1GUID = System.Text.Encoding.ASCII.GetString(DeviceGUIDasString1).ToString.Replace(vbNullChar, "").Trim
        Dim Joy2GUID = System.Text.Encoding.ASCII.GetString(DeviceGUIDasString2).ToString.Replace(vbNullChar, "").Trim

        lines(0) = MainformRef.Ver
        lines(1) = "Joystick=" & Joystick(0) & "|" & Joystick(1)
        lines(2) = "Deadzone=" & Deadzone(0) & "|" & Deadzone(1)
        lines(3) = "Peripheral=" & Peripheral(0) & "|" & Peripheral(1)

        Dim KeyCount = 4
        For Each _tab As TabPage In ControllersTab.TabPages
            For Each _cont As Control In _tab.Controls
                If TypeOf _cont Is TabControl Then ' Nested Controls Only used for mednafen right now
                    Dim _tc As TabControl = _cont
                    For Each _tab2 As TabPage In _tc.TabPages
                        For Each _cont2 As Control In _tab2.Controls
                            If TypeOf _cont2 Is keybindButton Then
                                Dim _btn As keybindButton = _cont2
                                If _btn.Emu = "nulldc" Then
                                    lines(KeyCount) = _btn.Name & "=" & _btn.KC(0) & "|" & _btn.KC(1)
                                ElseIf _btn.Emu = "mednafen" Then
                                    lines(KeyCount) = "med_" & _btn.ConfigString & "=" & _btn.KC(0) & "|" & _btn.KC(1)
                                End If
                                KeyCount += 1

                            End If
                        Next
                    Next
                End If
            Next
        Next

        File.WriteAllLines(MainformRef.NullDCPath & "\Controls.bear", lines)

    End Sub

    Private Sub LoadSettings()

        Dim configLines As String()

        If Not File.Exists(MainformRef.NullDCPath & "\Controls.bear") Then
            GenerateDefaults()
        End If

        configLines = File.ReadAllLines(MainformRef.NullDCPath & "\Controls.bear")

        RemoveHandler ControllerCB.SelectedIndexChanged, AddressOf ControllerCB_SelectedIndexChanged

        For Each line In configLines
            If line.StartsWith("Joystick") Then

                Joystick(0) = Convert.ToInt16(line.Split("=")(1).Split("|")(0))
                Joystick(1) = Convert.ToInt16(line.Split("=")(1).Split("|")(1))

                If ControllerCB.Items.Count > (Joystick(PlayerTab.SelectedIndex) + 1) Then
                    ControllerCB.SelectedIndex = Joystick(PlayerTab.SelectedIndex) + 1
                Else
                    ControllerCB.SelectedIndex = 0
                    Joystick(PlayerTab.SelectedIndex) = -1
                End If

            End If

            If line.StartsWith("Deadzone") Then
                Deadzone(0) = Convert.ToInt16(line.Split("=")(1).Split("|")(0))
                Deadzone(1) = Convert.ToInt16(line.Split("=")(1).Split("|")(1))
                DeadzoneTB.Value = Deadzone(PlayerTab.SelectedIndex)
                Deadzonetext.Text = "Deadzone: " & DeadzoneTB.Value

            End If

            If line.StartsWith("Peripheral") Then
                Peripheral(0) = Convert.ToInt16(line.Split("=")(1).Split("|")(0))
                Peripheral(1) = Convert.ToInt16(line.Split("=")(1).Split("|")(1))
                PeripheralCB.SelectedIndex = Peripheral(PlayerTab.SelectedIndex)
            End If

        Next

        AddHandler ControllerCB.SelectedIndexChanged, AddressOf ControllerCB_SelectedIndexChanged

        For Each _tab As TabPage In ControllersTab.TabPages
            For Each _cont As Control In _tab.Controls
                If TypeOf _cont Is TabControl Then
                    Dim _InnereTab As TabControl = _cont
                    For Each _tab2 As TabPage In _InnereTab.TabPages
                        For Each _cont2 As Control In _tab2.Controls
                            If TypeOf _cont2 Is keybindButton Then
                                Dim _btn As keybindButton = _cont2
                                GetKeyCodeFromFile(_btn, configLines)
                                AddHandler _btn.Click, AddressOf ClickedBindButton
                                _btn.UpdateTextFromPortID(PlayerTab.SelectedIndex)

                            End If
                        Next
                    Next
                End If

            Next
        Next

        If ControllerCB.SelectedIndex > -1 Then
            Joy = SDL_GameControllerOpen(ControllerCB.SelectedValue)
            Console.WriteLine(Joy)
        End If

        cbSDL.SelectedItem = MainformRef.ConfigFile.SDLVersion

    End Sub

    Private Sub GetKeyCodeFromFile(ByRef _btn As Button, ByRef _configlines As String())

        For Each line As String In _configlines

            If TypeOf _btn Is keybindButton Then
                Dim _btn2 As keybindButton = _btn

                If line.StartsWith(_btn2.Name & "=") Or line.StartsWith(_btn2.ConfigString & "=") Then
                    _btn2.KC(0) = line.Split("=")(1).Split("|")(0)
                    _btn2.KC(1) = line.Split("=")(1).Split("|")(1)
                    If _btn2.KC(0) = "" Then _btn2.KC(0) = "k0"
                    If _btn2.KC(1) = "" Then _btn2.KC(1) = "k0"

                End If

            End If
        Next

    End Sub

    Private Sub InputThread()
        Try
            While _InputThread.IsAlive

                Try
                    If Application.OpenForms().OfType(Of frmSDLMappingTool).Any Then
                        SDL_Delay(250)
                        Continue While
                    End If

                Catch ex As Exception
                    Console.WriteLine("yup")
                End Try

                SDL_Delay(16)

                Dim _event As SDL_Event
                While SDL_PollEvent(_event)
                    'Console.WriteLine(_event.type)
                    Select Case _event.type
                        Case 1616 ' Axis Motion
                            Console.WriteLine("Axis Motion: " & _event.caxis.axis & "|" & _event.caxis.axisValue)
                            Me.Invoke(Sub()

                                          Dim _deadzonetotal As Decimal = 32768 * Decimal.Divide(DeadzoneTB.Value, 100)
                                          Dim _axisnorm As Int32 = _event.caxis.axisValue
                                          _axisnorm = Math.Abs(_axisnorm)

                                          If _axisnorm > 256 And _axisnorm > _deadzonetotal Then
                                              If _event.caxis.axisValue > 0 Then
                                                  ButtonClicked("a" & _event.caxis.axis & "+", True)
                                              Else
                                                  ButtonClicked("a" & _event.caxis.axis & "-", True)
                                              End If

                                          Else
                                              ButtonsDown.Remove("a" & _event.caxis.axis & "+")
                                              ButtonsDown.Remove("a" & _event.caxis.axis & "-")
                                              ButtonClicked("a" & _event.caxis.axis & "+", False)
                                              ButtonClicked("a" & _event.caxis.axis & "-", False)

                                          End If

                                      End Sub)


                        Case 1617 ' Controller Button Down
                            'Console.WriteLine("Button Down: " & _event.cbutton.button & " | " & SDL_GameControllerNameForIndex(_event.cdevice.which))
                            Me.Invoke(Sub()
                                          ButtonClicked("b" & _event.cbutton.button, True)
                                      End Sub)

                        Case 1618 ' Controller Button Up
                            'Console.WriteLine("Button UP: " & _event.cbutton.button & " | " & SDL_GameControllerNameForIndex(_event.cdevice.which))
                            Me.Invoke(Sub()
                                          ButtonsDown.Remove("b" & _event.cbutton.button)
                                          ButtonClicked("b" & _event.cbutton.button, False)
                                      End Sub)

                        Case 1541 ' Connected
                            'Console.WriteLine("Connected: " & SDL_GameControllerNameForIndex(_event.jdevice.which))
                            Me.Invoke(Sub() UpdateControllersList())
                        Case 1542 ' Disconnected using Joystick event cuz other seems to only work with opened
                            'Console.WriteLine("Disconnected: " & SDL_JoystickNameForIndex(_event.jdevice.which))
                            Me.Invoke(Sub() UpdateControllersList())
                        Case 1538 ' Joystick HAT LEFT-(abs+1)/RIGHT+(abs+1) UP-(abs+2)/Down+(abs+2)
                            ' Actually what i think i'll do is let it show the controller mapping button in BEAR and then translate those 
                            ' to joystick buttons, not to confuse people why nulldc and mednafen have show different button numbers for the same button
                            ' This will also save me having to rewrite some shit
                        Case 1539 ' Joystick Button Down

                        Case 1540 ' Joystick Button Up

                        Case 1536 ' Joystick Axis

                        Case Else
                            Console.WriteLine("Unhandled SDL Event: " & _event.type)
                    End Select

                End While

                _event = Nothing
            End While
        Catch ex As Exception

        End Try


    End Sub

    ' Update Button Configs Based on the Config String for the Game Controller
    Public Sub AutoGenerateButtonConfigs(ByVal _configString As String)

        ' Check if we have Analog Controls Set
        If _configString.Contains("leftx") And _configString.Contains("lefty") Then

        Else ' We do not, so use DPAD Instead

        End If

    End Sub

    Public Sub UpdateControllersList()
        RemoveHandler ControllerCB.SelectedIndexChanged, AddressOf ControllerCB_SelectedIndexChanged
        SDL_GameControllerAddMappingsFromFile(MainformRef.NullDCPath & "\gamecontrollerdb.txt")
        SDL_GameControllerAddMappingsFromFile(MainformRef.NullDCPath & "\bearcontrollerdb.txt")

        Dim OldConnectedIndex = ControllerCB.SelectedValue
        Dim FoundController As Boolean = False
        AvailableControllersList.Rows.Clear()
        AvailableControllersList.Rows.Add({-1, "Keyboard Only"})

        For i = 0 To SDL_NumJoysticks() - 1

            AvailableControllersList.Rows.Add({i, "(" & i & ") " & SDL_JoystickNameForIndex(i)})

            If i = OldConnectedIndex Then
                FoundController = True
            End If

            'Console.WriteLine("Added: " & i & " | " & SDL_GameControllerNameForIndex(i))
        Next

        ControllerCB.DataSource = New DataView(AvailableControllersList)

        If FoundController Then
            ControllerCB.SelectedIndex = OldConnectedIndex + 1
        Else
            ControllerCB.SelectedIndex = 0
        End If

        AddHandler ControllerCB.SelectedIndexChanged, AddressOf ControllerCB_SelectedIndexChanged
    End Sub

    Private Sub Button34_Click(sender As Object, e As EventArgs)
        Me.Close()

    End Sub

    Private Sub frmKeyMapperSDL_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        _InputThread.Abort() ' Aight not accepting inputs anymore
        SaveSettings()

        ' Disable SDL completly we dun need that shit in the background no more
        For i = 0 To SDL_NumJoysticks() - 1
            If SDL_GameControllerGetAttached(SDL_GameControllerFromInstanceID(i)) = SDL_bool.SDL_TRUE Then
                Console.WriteLine("Disconnecting: " & SDL_GameControllerNameForIndex(i))
                SDL_GameControllerClose(SDL_GameControllerFromInstanceID(i))

            End If
        Next

        ' Update Control Configs for all the shit and hot-load if possible
        Dim ControlsConfigs() As String = File.ReadAllLines(MainformRef.NullDCPath & "\Controls.bear")
        Dim NaomiConfigs() As String = File.ReadAllLines(MainformRef.NullDCPath & "\nullDC.cfg")
        Dim DreamcastConfigs() As String = File.ReadAllLines(MainformRef.NullDCPath & "\dc\nullDC.cfg")
        Dim MednafenConfigs() As String = File.ReadAllLines(MainformRef.NullDCPath & "\mednafen\mednafen.cfg")

        ' Naomi Controls

        Dim linenumber = 0
        For Each line As String In NaomiConfigs
            If line.StartsWith("BPort") Then ' Check if it's a BEAR Port So we ignore everything else
                Dim player = 0 ' Default player 1 is index 0
                If line.StartsWith("BPortB") Then player = 1 ' This is port B so it's player 2 index 1

                Dim KeyFound = False
                For Each control_line As String In ControlsConfigs
                    If line.Contains(control_line.Split("=")(0) & "=") And control_line.Length > 0 Then
                        NaomiConfigs(linenumber) = line.Split("=")(0) & "=" & control_line.Split("=")(1).Split("|")(player)
                        KeyFound = True
                        Exit For
                    End If
                Next

                If Not KeyFound Then NaomiConfigs(linenumber) = line.Split("=")(0) & "=k0" ' The key was not in the controls so just set it to nothing

            End If
            linenumber += 1
        Next

        ' Dreamcast Controls

        ' We need the peripheral beforehand so we know which control settings to actually use
        Dim tempPeripheral As String() = {"", ""}
        Dim tempJoystick As String() = {"", ""}
        Dim TempDeadzone As String() = {"", ""}

        For Each line As String In ControlsConfigs
            If line.StartsWith("Peripheral=") Then
                tempPeripheral(0) = line.Split("=")(1).Split("|")(0)
                tempPeripheral(1) = line.Split("=")(1).Split("|")(1)
            End If
            If line.StartsWith("Joystick=") Then
                tempJoystick(0) = line.Split("=")(1).Split("|")(0)
                tempJoystick(1) = line.Split("=")(1).Split("|")(1)
            End If
            If line.StartsWith("Deadzone=") Then
                TempDeadzone(0) = line.Split("=")(1).Split("|")(0)
                TempDeadzone(1) = line.Split("=")(1).Split("|")(1)
            End If
        Next

        linenumber = 0
        For Each line As String In DreamcastConfigs ' Very similar to the Naomi configs, but different
            If line.StartsWith("BPort") Then
                Dim player = 0
                If line.StartsWith("BPortB") Then player = 1

                Dim KeyFound = False
                For Each control_line As String In ControlsConfigs

                    If tempPeripheral(player) = "1" Then ' Joystick Peripheral so get the STICK_ instead of the CONT_ setting
                        If control_line.StartsWith("STICK_") Then
                            If line.Contains(control_line.Split("=")(0).Replace("STICK_", "CONT_") & "=") And control_line.Length > 0 Then
                                DreamcastConfigs(linenumber) = line.Split("=")(0) & "=" & control_line.Split("=")(1).Split("|")(player)
                                KeyFound = True
                                Exit For
                            End If
                        End If

                    Else

                        If line.Contains(control_line.Split("=")(0) & "=") And control_line.Length > 0 Then
                            DreamcastConfigs(linenumber) = line.Split("=")(0) & "=" & control_line.Split("=")(1).Split("|")(player)
                            KeyFound = True
                            Exit For
                        End If

                    End If


                Next

                If Not KeyFound Then DreamcastConfigs(linenumber) = line.Split("=")(0) & "=k0"

                If line.StartsWith("BPortA_Joystick=") Then DreamcastConfigs(linenumber) = "BPortA_Joystick=" & tempJoystick(0)
                If line.StartsWith("BPortB_Joystick=") Then DreamcastConfigs(linenumber) = "BPortB_Joystick=" & tempJoystick(1)

                If line.StartsWith("BPortA_Deadzone=") Then DreamcastConfigs(linenumber) = "BPortA_Deadzone=" & TempDeadzone(0)
                If line.StartsWith("BPortB_Deadzone=") Then DreamcastConfigs(linenumber) = "BPortB_Deadzone=" & TempDeadzone(1)

            End If
            linenumber += 1

        Next

        ' Mednafen Controls

        GetMednafenControllerIDs()

        Dim _TranslatedControls(2) As Dictionary(Of String, String)
        For i = 0 To 1 ' Loop Through Controls to get translation and get number of axes since we'll need that to translate the dpad

            If Not Joystick(i) = -1 Then
                Dim _tmpJoy = SDL_JoystickOpen(i)
                Dim _numaxes = SDL_JoystickNumAxes(_tmpJoy)
                _TranslatedControls(i) = BEARButtonToMednafenButton(SDL_GameControllerMappingForGUID(SDL_JoystickGetDeviceGUID(Joystick(i))), _numaxes)
                SDL_JoystickClose(_tmpJoy)

            End If

        Next

        linenumber = 0
        For Each line As String In MednafenConfigs

            For Each control_line In ControlsConfigs

                If control_line.StartsWith("med_") Then ' Pretty much the only really consistent thing among all the mednafen controls
                    'joystick 0x00060079000000000000504944564944 button_1
                    'joystick 0x00060079000000000000504944564944 abs_1+
                    'keyboard 0x0 18
                    control_line = control_line.Substring(4)
                    Dim tmpControlString = ""

                    If line.StartsWith(control_line.Split("=")(0).Replace("<port>", "1")) Or line.StartsWith(control_line.Split("=")(0).Replace("<port>", "2")) Then
                        Dim _player = 1
                        If line.StartsWith(control_line.Split("=")(0).Replace("<port>", "2")) Then _player = 2

                        tmpControlString += control_line.Split("=")(0).Replace("<port>", _player.ToString) ' Initial String
                        Dim _KeyCode = control_line.Split("=")(1).Split("|")(_player - 1)

                        If _KeyCode.StartsWith("k") Then ' Keyboard

                            If Not _KeyCode = "k0" Then
                                tmpControlString += " keyboard 0x0 " & KeyCodeToSDLScanCode(control_line.Split("=")(1).Split("|")(_player - 1).Substring(1))
                            End If

                        Else ' Joystick

                            tmpControlString += " Joystick " & MednafenControllerID(Joystick(_player - 1))
                            If _TranslatedControls(_player - 1).ContainsKey(_KeyCode) Then
                                tmpControlString += " " & _TranslatedControls(_player - 1)(_KeyCode)
                            Else
                                tmpControlString = control_line.Split("=")(0).Replace("<port>", _player.ToString)
                            End If

                        End If

                    End If

                    If Not tmpControlString = "" Then MednafenConfigs(linenumber) = tmpControlString

                End If
            Next

            linenumber += 1
        Next

        ' Save all the changes

        File.SetAttributes(MainformRef.NullDCPath & "\nullDC.cfg", FileAttributes.Normal)
        File.WriteAllLines(MainformRef.NullDCPath & "\nullDC.cfg", NaomiConfigs)

        File.SetAttributes(MainformRef.NullDCPath & "\dc\nullDC.cfg", FileAttributes.Normal)
        File.WriteAllLines(MainformRef.NullDCPath & "\dc\nullDC.cfg", DreamcastConfigs)

        File.SetAttributes(MainformRef.NullDCPath & "\mednafen\mednafen.cfg", FileAttributes.Normal)
        File.WriteAllLines(MainformRef.NullDCPath & "\mednafen\mednafen.cfg", MednafenConfigs)


        ' Check if nullDC is running to hotload the settings
        If MainformRef.IsNullDCRunning Then
            If platform = "na" Then PostMessage(MainformRef.NullDCLauncher.NullDCproc.MainWindowHandle, &H111, 141, 0)
            If platform = "dc" Then PostMessage(MainformRef.NullDCLauncher.NullDCproc.MainWindowHandle, &H111, 144, 0) ' 180 144

        End If


        ' Finally Turn Off SDL
        SDL_Quit() ' Turn off SDL make sure nothing changes before we write the controls to the configs.

    End Sub

    Private Sub frmKeyMapperSDL_KeyDown(sender As Object, e As KeyEventArgs) Handles MyBase.KeyDown
        ButtonClicked("k" & e.KeyCode, True)

    End Sub

    Private Sub frmKeyMapperSDL_KeyUp(sender As Object, e As KeyEventArgs) Handles MyBase.KeyUp
        ButtonsDown.Remove("k" & e.KeyCode)
        ButtonClicked("k" & e.KeyCode, False)
    End Sub

    Private Sub DeadzoneTB_MouseCaptureChanged(sender As Object, e As EventArgs)
        Deadzone(PlayerTab.SelectedIndex) = DeadzoneTB.Value
        SaveSettings()

    End Sub

    Private Sub ClickedBindButton(sender As Button, e As EventArgs)
        'Console.WriteLine("Bind clicked: " & sender.Name)
        If Not Currently_Binding Is Nothing Then
            Currently_Binding.BackColor = Color.White
            Currently_Binding = Nothing
        End If

        Currently_Binding = sender
        Currently_Binding.BackColor = Color.Red
        ActiveControl = Nothing

    End Sub

    Private Sub PlayerTab_SelectedIndexChanged(sender As Object, e As EventArgs)
        RemoveHandler ControllerCB.SelectedIndexChanged, AddressOf ControllerCB_SelectedIndexChanged
        RemoveHandler DeadzoneTB.MouseCaptureChanged, AddressOf DeadzoneTB_MouseCaptureChanged
        RemoveHandler PeripheralCB.SelectedIndexChanged, AddressOf PeripheralCB_SelectedIndexChanged

        If Not Joy = Nothing Then
            If SDL_GameControllerGetAttached(Joy) = SDL_bool.SDL_TRUE Then
                SDL_GameControllerClose(Joy)
            End If
            Joy = Nothing
        End If

        If Joystick(PlayerTab.SelectedIndex) < ControllerCB.Items.Count - 1 Then
            ControllerCB.SelectedIndex = Joystick(PlayerTab.SelectedIndex) + 1
        Else
            ControllerCB.SelectedIndex = 0
            Joystick(PlayerTab.SelectedIndex) = -1
        End If

        DeadzoneTB.Value = Deadzone(PlayerTab.SelectedIndex)
        PeripheralCB.SelectedIndex = Peripheral(PlayerTab.SelectedIndex)

        If Not Joystick(PlayerTab.SelectedIndex) = -1 Then
            Joy = SDL_GameControllerOpen(Joystick(PlayerTab.SelectedIndex))
        End If

        AddHandler ControllerCB.SelectedIndexChanged, AddressOf ControllerCB_SelectedIndexChanged
        AddHandler DeadzoneTB.MouseCaptureChanged, AddressOf DeadzoneTB_MouseCaptureChanged
        AddHandler PeripheralCB.SelectedIndexChanged, AddressOf PeripheralCB_SelectedIndexChanged

        ActiveControl = Nothing
        UpdateButtonLabels()

    End Sub

    Private Sub btn_Close_Click(sender As Object, e As EventArgs) Handles btn_Close.Click
        Me.Close()

    End Sub

    Private Sub ControllerCB_SelectedIndexChanged(sender As Object, e As EventArgs)

        'Console.Write("Changing Controller: ")
        If Not Joy = Nothing Then
            If SDL_GameControllerGetAttached(Joy) = SDL_bool.SDL_TRUE Then
                SDL_GameControllerClose(Joy)
            End If
            Joy = Nothing
        End If

        If ControllerCB.SelectedValue >= 0 Then
            'Console.Write(ControllerCB.SelectedValue & " | ")
            Joy = SDL_GameControllerOpen(ControllerCB.SelectedValue)
            'Console.WriteLine(SDL_GameControllerName(Joy))
        Else
            'Console.WriteLine("Keyboard")
        End If

        'Console.WriteLine("joystick: " & ControllerCB.SelectedValue)
        Joystick(PlayerTab.SelectedIndex) = ControllerCB.SelectedValue

        SaveSettings()
    End Sub

    Private Sub ButtonClicked(ByVal _keycode As String, ByVal _down As Boolean)
        If Not ButtonsDown.ContainsKey(_keycode) Then
            If Not Currently_Binding Is Nothing And (_down Or _keycode = "k13") Then
                If TypeOf Currently_Binding Is keybindButton Then
                    Dim _tmp As keybindButton = Currently_Binding
                    _tmp.ChangeKeyCode(_keycode, PlayerTab.SelectedIndex)
                    _tmp.BackColor = Color.White
                End If

                Currently_Binding = Nothing
                ActiveControl = Nothing
                SaveSettings()

            End If

            ' rn all this does is highlight
            For Each _btns As Control In ControllersTab.SelectedTab.Controls
                If TypeOf _btns Is TabControl Then
                    Dim _innerTabControl As TabControl = _btns
                    For Each _innercontrols In _innerTabControl.SelectedTab.Controls

                        If TypeOf _innercontrols Is keybindButton Then
                            Dim _btn As keybindButton = _innercontrols
                            If _btn.KC(PlayerTab.SelectedIndex) = _keycode Then
                                If _down Then
                                    _btn.BackColor = Color.Green

                                Else
                                    _btn.BackColor = Color.White

                                End If
                            End If
                        End If

                    Next

                End If


            Next

            If _down Then
                ButtonsDown.Add(_keycode, True)
            End If

        End If
    End Sub

    Private Sub PeripheralCB_SelectedIndexChanged(sender As Object, e As EventArgs)
        Peripheral(PlayerTab.SelectedIndex) = PeripheralCB.SelectedIndex
        If PlayerTab.SelectedIndex = 0 Then
            MainformRef.ConfigFile.Peripheral = PeripheralCB.SelectedIndex
            MainformRef.ConfigFile.SaveFile(False)
        End If

        SaveSettings()
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles btnSDL.Click
        If ControllerCB.SelectedIndex = 0 Then
            MsgBox("Select a CONTROLLER from the list first. For Keyboard just click a button on the right, when it turns red press a key on your keyboard.")
        Else
            frmSDLMappingTool.ShowDialog(Me)
        End If

    End Sub

    Private Sub frmKeyMapperSDL_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Icon = My.Resources.NewNullDCBearIcon

    End Sub

    Private Sub GetMednafenControllerIDs()
        Dim MedProc As Process = New Process()
        MedProc.StartInfo.FileName = MainformRef.NullDCPath & "\mednafen\mednafen.exe"
        MedProc.StartInfo.EnvironmentVariables.Add("MEDNAFEN_NOPOPUPS", "1")
        MedProc.StartInfo.CreateNoWindow = True
        MedProc.StartInfo.UseShellExecute = False
        MedProc.StartInfo.Arguments = "Hi"
        MedProc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden

        MedProc.Start()
        MedProc.WaitForExit()

        Dim ControllerIndex = 0
        For Each line As String In File.ReadAllLines(MainformRef.NullDCPath & "\mednafen\stdout.txt")
            If line.StartsWith("  ID: ") Then
                MednafenControllerID(ControllerIndex) = line.Trim.Split(" ")(1)
                Console.WriteLine("Found Medanfen Controller ID: " & line.Trim.Split(" ")(1))
                ControllerIndex += 1
            End If
        Next

        Console.WriteLine("Ran Mednafen once to get the controls output")

    End Sub

    Private Sub btnResetAll_Click(sender As Object, e As EventArgs) Handles btnResetAll.Click
        Dim result1 As DialogResult = MessageBox.Show("This will Reset ALL the controls, k?", "Reset All?", MessageBoxButtons.YesNo)

        If result1 = DialogResult.Yes Then
            GenerateDefaults()
            LoadSettings()
            If File.Exists(MainformRef.NullDCPath & "\bearcontrollerdb.txt") Then File.Delete(MainformRef.NullDCPath & "\bearcontrollerdb.txt")
            If File.Exists(MainformRef.NullDCPath & "\dc\bearcontrollerdb.txt") Then File.Delete(MainformRef.NullDCPath & "\dc\bearcontrollerdb.txt")
            UpdateControllersList()
            Me.Close()

        End If

    End Sub
End Class

Public Class KeyBind
    Public KC As String
    Public Name As String
    Public Button As Button
    Public Platform As String

    Public Sub New(ByVal _kc As String, ByVal _name As String, ByRef _frm As frmKeyMapperSDL, ByVal _plat As String)
        KC = _kc
        Name = _name
        Button = _frm.Controls.Find(_name, True)(0)
        Platform = _plat

        If Button Is Nothing Then
            MsgBox("button not found: " & _name)
        End If

    End Sub

End Class