#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.NativeUI;
using FTOptix.UI;
using FTOptix.WebUI;
using FTOptix.CoreBase;
using FTOptix.Alarm;
using FTOptix.Recipe;
using FTOptix.EventLogger;
using FTOptix.DataLogger;
using FTOptix.SQLiteStore;
using FTOptix.Store;
using FTOptix.Report;
using FTOptix.OPCUAServer;
using FTOptix.CODESYS;
using FTOptix.Modbus;
using FTOptix.Retentivity;
using FTOptix.CommunicationDriver;
using FTOptix.UI;
using FTOptix.Core;
#endregion

public class ResetPdfReport : BaseNetLogic
{
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
        Project.Current.GetVariable("Reports/Reports1/PDFGeneratedHandler1/EventArguments/Result").Value = 99;
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }
}
