#region Using directives
using FTOptix.NetLogic;
using UAManagedCore;
using FTOptix.UI;
#endregion

public class CSVExportOutputMessageLogic : BaseNetLogic {
    public override void Start() {
        messageVariable = Owner.GetVariable("Message");
        task = new DelayedTask(() => {
            if (messageVariable == null) {
                Log.Error("Unable to find variable Message in LoginFormOutputMessage label");
                return;
            }

            messageVariable.Value = "";
            taskStarted = false;
        }, 5000, LogicObject);
    }

    public override void Stop() {
        task?.Dispose();
    }
    [ExportMethod]
    public void SetOutputMessage(string message) {
        if (messageVariable == null) {
            Log.Error("Unable to find variable Message in LoginFormOutputMessage label");
            return;
        }

        messageVariable.Value = message;

        if (taskStarted) {
            task?.Cancel();
            taskStarted = false;
        }

        task.Start();
        taskStarted = true;
    }

    DelayedTask task;
    bool taskStarted = false;
    IUAVariable messageVariable;

}
