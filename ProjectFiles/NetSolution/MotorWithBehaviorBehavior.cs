#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.UI;
using FTOptix.NativeUI;
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
using FTOptix.Core;
#endregion

[CustomBehavior]
public class MotorWithBehaviorBehavior : BaseNetBehavior
{
    public override void Start()
    {
        // Insert code to be executed when the user-defined behavior is started
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined behavior is stopped
    }

    [ExportMethod]
    public virtual void StartMethod() {
        Log.Info(System.Reflection.MethodBase.GetCurrentMethod().Name + " executed on " + Log.Node(Node));
        Node.Speed = +16;
        Node.Acceleration = +9;
        Node.PowerOn = true;
    }

    [ExportMethod]
    public virtual void StopMethod() {
        Log.Info(System.Reflection.MethodBase.GetCurrentMethod().Name + " executed on " + Log.Node(Node));
        Node.Speed = 0;
        Node.Acceleration = 0;
        Node.PowerOn = false;
    }

    #region Auto-generated code, do not edit!
    protected new MotorWithBehavior Node => (MotorWithBehavior)base.Node;
#endregion
}
