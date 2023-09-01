#region Using directives
using FTOptix.NetLogic;
using System;
using UAManagedCore;
using FTOptix.UI;
#endregion

public class ClockLogic : BaseNetLogic {
    public override void Start() {
        periodicTask = new PeriodicTask(UpdateTime, 1000, LogicObject);
        periodicTask.Start();
    }

    public override void Stop() {
        periodicTask.Dispose();
        periodicTask = null;
    }

    private void UpdateTime() {
        var timeVar = LogicObject.GetVariable("Time");
        timeVar.Value = DateTime.Now;
    }

    private PeriodicTask periodicTask;
}
