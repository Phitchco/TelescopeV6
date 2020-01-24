//*** CHECK THIS ProgID ***
var X = new ActiveXObject("ASCOM.HPL_Nano_V1.Focuser");
WScript.Echo("This is " + X.Name + ")");
// You may want to uncomment this...
// X.Connected = true;
X.SetupDialog();
