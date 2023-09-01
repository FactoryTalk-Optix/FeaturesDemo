#region Using directives
using FTOptix.Alarm;
using FTOptix.Core;
using FTOptix.CoreBase;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using UAManagedCore;
using FTOptix.UI;
#endregion

public class CreateDeleteAlarm : BaseNetLogic {
    [ExportMethod]
    public void CreateAlarm(string alarmName, string alarmMessage, string inputVariable) {
        var variable = Project.Current.GetVariable("Model/PrototypesInstances/RuntimeAlarms/" + inputVariable);
        var myAlarm = InformationModel.MakeObject<DigitalAlarm>(alarmName);
        myAlarm.InputValueVariable.SetDynamicLink(variable, DynamicLinkMode.Read);
        myAlarm.NormalStateValue = 0;
        myAlarm.Message = alarmMessage;
        Project.Current.Get<Folder>("Alarms/RuntimeAlarms").Add(myAlarm);
    }

    [ExportMethod]
    public void DeleteAlarm(string alarmName) {
        Project.Current.Get<Folder>("Alarms/RuntimeAlarms").Remove(Project.Current.Get<DigitalAlarm>("Alarms/RuntimeAlarms/" + alarmName));
    }
}
