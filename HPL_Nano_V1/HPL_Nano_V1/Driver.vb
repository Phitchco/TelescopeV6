'tabs=4
' --------------------------------------------------------------------------------
' ASCOM Focuser driver for HPL_Nano_V1
'
'
' Implements:	ASCOM Focuser interface version: 1.0
' Author:		Paul  paul-hitchcock@comcast.net
'
' Edit Log:
'
' Date			Who	Vers	Description
' -----------	---	-----	-------------------------------------------------------
' 06/19/2019	PRH	1.2	Initial edit, from Focuser template with additions
' ---------------------------------------------------------------------------------
'
'
' Your driver's ID is ASCOM.HPL_Nano_V1.Focuser
'
' The Guid attribute sets the CLSID for ASCOM.DeviceName.Focuser
' The ClassInterface/None addribute prevents an empty interface called
' _Focuser from being created and used as the [default] interface
'

' This definition is used to select code that's only applicable for one device type
#Const Device = "Focuser"

Imports ASCOM
Imports ASCOM.Astrometry
Imports ASCOM.Astrometry.AstroUtils
Imports ASCOM.DeviceInterface
Imports ASCOM.Utilities

Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Globalization
Imports System.Runtime.InteropServices
Imports System.Text

<Guid("fdee47b4-a799-4136-ac32-7e4a3c028bd8")>
<ClassInterface(ClassInterfaceType.None)>
Public Class Focuser

    ' The Guid attribute sets the CLSID for ASCOM.HPL_Nano_V1.Focuser
    ' The ClassInterface/None addribute prevents an empty interface called
    ' _HPL_Nano_V1 from being created and used as the [default] interface

    ' TODO Replace the not implemented exceptions with code to implement the function or
    ' throw the appropriate ASCOM exception.
    '
    Implements IFocuserV3

    Friend Shared driverID As String = "ASCOM.HPL_Nano_V1.Focuser"
    Private Shared driverDescription As String = "HPL_Nano_V1 Focuser"
    Private Shared objSerial As ASCOM.Utilities.Serial

    Friend Shared comPortProfileName As String = "COM Port" 'Constants used for Profile persistence
    Friend Shared traceStateProfileName As String = "Trace Level"
    Friend Shared comPortDefault As String = "COM1"
    Friend Shared traceStateDefault As String = "True"

    Friend Shared comPort As String ' Variables to hold the currrent device configuration
    Friend Shared traceState As Boolean
    Friend Shadows portNum As String

    Private connectedState As Boolean ' Private variable to hold the connected state
    Private utilities As Util ' Private variable to hold an ASCOM Utilities object
    Private astroUtilities As AstroUtils ' Private variable to hold an AstroUtils object to provide the Range method
    Private TL As TraceLogger ' Private variable to hold the trace logger object (creates a diagnostic log file with information that you specify)

    Friend Shared ReceiveTimeout = 15

    '
    ' Constructor - Must be public for COM registration!
    '
    Public Sub New()

        ReadProfile() ' Read device configuration from the ASCOM Profile store
        TL = New TraceLogger("", "HPL_Nano_V1")
        TL.Enabled = traceState
        TL.LogMessage("Focuser", "Starting initialisation")

        connectedState = False ' Initialise connected to false
        utilities = New Util() ' Initialise util object
        astroUtilities = New AstroUtils 'Initialise new astro utiliites object

        'TODO: Implement your additional construction here

        TL.LogMessage("Focuser", "Completed initialisation")
    End Sub

#Region "Common properties and methods"
    ''' <summary>
    ''' Displays the Setup Dialog form.
    ''' If the user clicks the OK button to dismiss the form, then
    ''' the new settings are saved, otherwise the old values are reloaded.
    ''' THIS IS THE ONLY PLACE WHERE SHOWING USER INTERFACE IS ALLOWED!
    ''' </summary>
    Public Sub SetupDialog() Implements IFocuserV3.SetupDialog
        ' consider only showing the setup dialog if not connected
        ' or call a different dialog if connected
        If IsConnected Then
            System.Windows.Forms.MessageBox.Show("Already connected, just press OK")
        End If

        Using F As SetupDialogForm = New SetupDialogForm()
            Dim result As System.Windows.Forms.DialogResult = F.ShowDialog()
            If result = DialogResult.OK Then
                WriteProfile() ' Persist device configuration values to the ASCOM Profile store
            End If
        End Using
    End Sub

    Public ReadOnly Property SupportedActions() As ArrayList Implements IFocuserV3.SupportedActions
        Get
            TL.LogMessage("SupportedActions Get", "Returning empty arraylist")
            Return New ArrayList()
        End Get
    End Property

    Public Function Action(ByVal ActionName As String, ByVal ActionParameters As String) As String Implements IFocuserV3.Action
        Throw New ActionNotImplementedException("Action " & ActionName & " is not supported by this driver")
    End Function

    Public Sub CommandBlind(ByVal Command As String, Optional ByVal Raw As Boolean = False) Implements IFocuserV3.CommandBlind
        CheckConnected("CommandBlind")
        ' Call CommandString and return as soon as it finishes
        Me.CommandString(Command, Raw)
        ' or
        Throw New MethodNotImplementedException("CommandBlind")
    End Sub

    Public Function CommandBool(ByVal Command As String, Optional ByVal Raw As Boolean = False) As Boolean _
        Implements IFocuserV3.CommandBool
        CheckConnected("CommandBool")
        Dim ret As String = CommandString(Command, Raw)
        ' TODO decode the return string and return true or false
        ' or
        Throw New MethodNotImplementedException("CommandBool")
    End Function

    Public Function CommandString(ByVal Command As String, Optional ByVal Raw As Boolean = False) As String _
        Implements IFocuserV3.CommandString
        CheckConnected("CommandString")
        ' it's a good idea to put all the low level communication with the device here,
        ' then all communication calls this function
        ' you need something to ensure that only one command is in progress at a time
        Throw New MethodNotImplementedException("CommandString")
    End Function

    Public Property Connected() As Boolean Implements IFocuserV3.Connected
        Get
            TL.LogMessage("Connected Get", IsConnected.ToString())
            Return IsConnected
        End Get

        Set(value As Boolean)
            TL.LogMessage("Connected Set", value.ToString())
            If value = IsConnected Then
                Return
            End If


            '  If value = True Then
            '  Dim comPort As String = My.Settings.CommPort
            '  connectedState = True
            '  TL.LogMessage("Connected Set", "Connecting to port " + comPort)
            '  objSerial = New ASCOM.Utilities.Serial
            '  objSerial.Port = 20
            '  objSerial.Speed = 9600
            '  objSerial.Connected = True
            '  objSerial.ReceiveTimeout = 4
            '  Else
            '  connectedState = False
            '  TL.LogMessage("Connected Set", "Disconnecting from port " + comPort)
            '  objSerial.Connected = False
            '  objSerial.ClearBuffers()
            '  objSerial.Dispose()
            '  objSerial = Nothing
            '  End If

            If value Then
                connectedState = True
                TL.LogMessage("Connected Set", "Connecting to port " + comPort)
                portNum = Right(comPort, Len(comPort) - 3)
                objSerial = New ASCOM.Utilities.Serial
                objSerial.Port = CInt(portNum)
                objSerial.Speed = SerialSpeed.ps9600
                objSerial.Connected = True
            Else
                connectedState = False
                TL.LogMessage("Connected Set", "Disconnecting from port " + comPort)
                objSerial.Connected = False
                objSerial = Nothing
            End If
        End Set
    End Property

    Public ReadOnly Property Description As String Implements IFocuserV3.Description
        Get
            ' this pattern seems to be needed to allow a public property to return a private field
            Dim d As String = driverDescription
            TL.LogMessage("Description Get", d)
            Return d
        End Get
    End Property

    Public ReadOnly Property DriverInfo As String Implements IFocuserV3.DriverInfo
        Get
            Dim m_version As Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version
            ' TODO customise this driver description
            Dim s_driverInfo As String = "Information about the driver itself. Version: " + m_version.Major.ToString() + "." + m_version.Minor.ToString()
            TL.LogMessage("DriverInfo Get", s_driverInfo)
            Return s_driverInfo
        End Get
    End Property

    Public ReadOnly Property DriverVersion() As String Implements IFocuserV3.DriverVersion
        Get
            ' Get our own assembly and report its version number
            ' TL.LogMessage("DriverVersion Get", Reflection.Assembly.GetExecutingAssembly.GetName.Version.ToString(2))
            'Return Reflection.Assembly.GetExecutingAssembly.GetName.Version.ToString(2)
            Dim s_name As String
            objSerial.Transmit("V#")
            s_name = objSerial.ReceiveTerminated("#")
            s_name = s_name.Replace("#", "")
            TL.LogMessage("DriverVersion", s_name)
            Return s_name ' Return the version and date of focuser firmware
        End Get
    End Property

    Public ReadOnly Property InterfaceVersion() As Short Implements IFocuserV3.InterfaceVersion
        Get
            TL.LogMessage("InterfaceVersion Get", "3")
            Return 3
        End Get
    End Property

    Public ReadOnly Property Name As String Implements IFocuserV3.Name
        Get
            Dim s_name As String = "Short driver name - please customise"
            TL.LogMessage("Name Get", s_name)
            Return driverDescription
            Return s_name
        End Get
    End Property

    Public Sub Dispose() Implements IFocuserV3.Dispose
        ' Clean up the tracelogger and util objects
        TL.Enabled = False
        TL.Dispose()
        TL = Nothing
        utilities.Dispose()
        utilities = Nothing
        astroUtilities.Dispose()
        astroUtilities = Nothing
    End Sub

#End Region

#Region "IFocuser Implementation"

    Private focuserPosition As Integer = 0 ' Class level variable to hold the current focuser position
    Private Const focuserSteps As Integer = 20000

    Public ReadOnly Property Absolute() As Boolean Implements IFocuserV3.Absolute
        Get
            TL.LogMessage("Absolute Get", True.ToString())
            Return True ' This is an absolute focuser
        End Get
    End Property

    Public Sub Halt() Implements IFocuserV3.Halt
        TL.LogMessage("Halt", "Not implemented")
        '      Throw New ASCOM.MethodNotImplementedException("Halt")
    End Sub

    Public ReadOnly Property IsMoving() As Boolean Implements IFocuserV3.IsMoving
        Get
            TL.LogMessage("IsMoving Get", False.ToString())
            Return False ' This focuser always moves instantaneously so no need for IsMoving ever to be True
        End Get
    End Property

    Public Property Link() As Boolean Implements IFocuserV3.Link
        Get
            TL.LogMessage("Link Get", Me.Connected.ToString())
            Return Me.Connected ' Direct function to the connected method, the Link method is just here for backwards compatibility
        End Get
        Set(value As Boolean)
            TL.LogMessage("Link Set", value.ToString())
            Me.Connected = value ' Direct function to the connected method, the Link method is just here for backwards compatibility
        End Set
    End Property

    Public ReadOnly Property MaxIncrement() As Integer Implements IFocuserV3.MaxIncrement
        Get
            TL.LogMessage("MaxIncrement Get", focuserSteps.ToString())
            Return focuserSteps ' Maximum change in one move
        End Get
    End Property

    Public ReadOnly Property MaxStep() As Integer Implements IFocuserV3.MaxStep
        Get
            TL.LogMessage("MaxStep Get", focuserSteps.ToString())
            Return focuserSteps ' Maximum extent of the focuser, so position range is 0 to 50,000
        End Get
    End Property

    Public Sub Move(Position As Integer) Implements IFocuserV3.Move
        TL.LogMessage("Move", Position.ToString())
        Dim M As String = "M"
        M = "M" + Position.ToString + "#"
        objSerial.Transmit(M)                   ' Set the focuser position
        objSerial.ReceiveTerminated("#")        ' wait for # to be returned
        '        focuserPosition = Position ' Set the focuser position
    End Sub

    Public ReadOnly Property Position() As Integer Implements IFocuserV3.Position
        Get
            objSerial.Transmit("G#")
            Dim Pos As String
            Pos = objSerial.ReceiveTerminated("#")
            Pos = Pos.Replace("#", "")
            TL.LogMessage("Position", Pos.ToString())
            Return Pos ' Return the focuser position
        End Get
    End Property

    Public ReadOnly Property StepSize() As Double Implements IFocuserV3.StepSize
        Get
            TL.LogMessage("StepSize Get", "Not implemented")
            Throw New ASCOM.PropertyNotImplementedException("StepSize", False)
        End Get
    End Property

    Public Property TempComp() As Boolean Implements IFocuserV3.TempComp
        Get
            TL.LogMessage("TempComp Get", False.ToString())
            Return False
        End Get
        Set(value As Boolean)
            TL.LogMessage("TempComp Set", "Not implemented")
            Throw New ASCOM.PropertyNotImplementedException("TempComp", True)
        End Set
    End Property

    Public ReadOnly Property TempCompAvailable() As Boolean Implements IFocuserV3.TempCompAvailable
        Get
            TL.LogMessage("TempCompAvailable Get", False.ToString())
            Return False ' Temperature compensation is not available in this driver
        End Get
    End Property

    Public ReadOnly Property Temperature() As Double Implements IFocuserV3.Temperature
        Get
            Dim T As String
            objSerial.Transmit("T#")
            T = objSerial.ReceiveTerminated("#")
            T = T.Replace("#", "")
            TL.LogMessage("Temperature Get", T)
            Return T ' Return the focuser position
            '            Throw New ASCOM.PropertyNotImplementedException("Temperature", False)
        End Get
    End Property

#End Region

#Region "Private properties and methods"
    ' here are some useful properties and methods that can be used as required
    ' to help with

#Region "ASCOM Registration"

    Private Shared Sub RegUnregASCOM(ByVal bRegister As Boolean)

        Using P As New Profile() With {.DeviceType = "Focuser"}
            If bRegister Then
                P.Register(driverID, driverDescription)
            Else
                P.Unregister(driverID)
            End If
        End Using

    End Sub

    <ComRegisterFunction()>
    Public Shared Sub RegisterASCOM(ByVal T As Type)

        RegUnregASCOM(True)

    End Sub

    <ComUnregisterFunction()>
    Public Shared Sub UnregisterASCOM(ByVal T As Type)

        RegUnregASCOM(False)

    End Sub

#End Region

    ''' <summary>
    ''' Returns true if there is a valid connection to the driver hardware
    ''' </summary>
    Private ReadOnly Property IsConnected As Boolean
        Get
            If Not objSerial Is Nothing Then
                If objSerial.Connected Then
                    Return True
                Else
                    Return False
                End If
            Else
                Return False
            End If
            ' TODO check that the driver hardware connection exists and is connected to the hardware
            Return connectedState
        End Get
    End Property

    ''' <summary>
    ''' Use this function to throw an exception if we aren't connected to the hardware
    ''' </summary>
    ''' <param name="message"></param>
    Private Sub CheckConnected(ByVal message As String)
        If Not IsConnected Then
            Throw New NotConnectedException(message)
        End If
    End Sub

    ''' <summary>
    ''' Read the device configuration from the ASCOM Profile store
    ''' </summary>
    Friend Sub ReadProfile()
        Using driverProfile As New Profile()
            driverProfile.DeviceType = "Focuser"
            traceState = Convert.ToBoolean(driverProfile.GetValue(driverID, traceStateProfileName, String.Empty, traceStateDefault))
            comPort = driverProfile.GetValue(driverID, comPortProfileName, String.Empty, comPortDefault)
        End Using
    End Sub

    ''' <summary>
    ''' Write the device configuration to the  ASCOM  Profile store
    ''' </summary>
    Friend Sub WriteProfile()
        Using driverProfile As New Profile()
            driverProfile.DeviceType = "Focuser"
            driverProfile.WriteValue(driverID, traceStateProfileName, traceState.ToString())
            driverProfile.WriteValue(driverID, comPortProfileName, comPort.ToString())
        End Using

    End Sub

#End Region

End Class
