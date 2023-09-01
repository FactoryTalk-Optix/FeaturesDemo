#region Using directives
using FTOptix.Alarm;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using System;
using System.Collections.Generic;
using UAManagedCore;
using FTOptix.UI;
using OpcUa = UAManagedCore.OpcUa;
#endregion

public class NonExclusiveRateOfChangeAlarmWithEmailNotificationLogic : BaseNetLogic, IUAEventObserver {
    public override void Start() {
        alarmObject = (AlarmController)Owner;
        previousActiveState = GetInitialActiveState();
        eventRegistration = alarmObject.RegisterUAEventObserver(this, OpcUa.ObjectTypes.AlarmConditionType);

        emailUserNode = GetAlarmProperty("EmailUser");
        if (emailUserNode == null)
            return;

        var emailVariable = emailUserNode.GetVariable("Email");
        if (emailVariable == null) {
            Log.Error("NonExclusiveRateOfChangeAlarmWithEmailNotificationLogic", $"Could not find Email variable of {emailUserNode.BrowseName} user");
            return;
        }

        email = emailVariable.Value;
        if (string.IsNullOrEmpty(email)) {
            Log.Error("NonExclusiveRateOfChangeAlarmWithEmailNotificationLogic", $"Email variable property missing in user {Log.Node(emailUserNode)} set in {Log.Node(alarmObject)}");
            return;
        }
    }

    public override void Stop() {
        eventRegistration?.Dispose();
    }

    public void OnEvent(IUAObject eventNotifier, IUAObjectType eventType, IReadOnlyList<object> args, ulong senderId) {
        var eventArguments = eventType.EventArguments;
        var currentActiveState = (bool)eventArguments.GetFieldValue(args, "ActiveState/Id");
        if (!IsAlarmTransitioningFromInactiveState(currentActiveState))
            return;

        var alarmNode = InformationModel.Get<AlarmController>(eventNotifier.NodeId);
        var alarmMessage = ConstructAlarmEmailBody(eventArguments, args, alarmNode);
        SendAlarmEmail(alarmNode.BrowseName, alarmMessage);
    }

    private bool GetInitialActiveState() {
        var retainedAlarmsNode = InformationModel.Get(FTOptix.Alarm.Objects.RetainedAlarms);

        var retainedAlarm = retainedAlarmsNode.Find(alarmObject.BrowseName);
        if (retainedAlarm == null)
            return false;

        return retainedAlarm.GetVariable("ActiveState/Id").Value;
    }

    private bool IsAlarmTransitioningFromInactiveState(bool currentActiveState) {
        if (!previousActiveState && currentActiveState) {
            previousActiveState = currentActiveState;
            return true;
        }

        previousActiveState = currentActiveState;
        return false;
    }

    private string ConstructAlarmEmailBody(IEventArguments eventArguments, IReadOnlyList<object> args, AlarmController alarmcontroller) {
        var alarmTimestamp = ((DateTime)eventArguments.GetFieldValue(args, "Time")).ToString("O");
        var alarmAckedState = eventArguments.GetFieldValue(args, "AckedState/Id").ToString();
        var alarmConfirmedState = eventArguments.GetFieldValue(args, "ConfirmedState/Id").ToString();

        var alarmMessage = $"Alarm name: {alarmcontroller.BrowseName}\n" +
            $"Message: {alarmcontroller.Message}\n" +
            $"Timestamp: {alarmTimestamp}\n" +
            $"Acked: {alarmAckedState}\n" +
            $"Confirmed: {alarmConfirmedState}";

        return alarmMessage;
    }

    private void SendAlarmEmail(string alarmBrowseName, string alarmMessage) {
        var emailSender = GetAlarmProperty("EmailSender") as NetLogicObject;
        if (emailSender == null) {
            Log.Error("NonExclusiveRateOfChangeAlarmWithEmailNotificationLogic", "Could not send email: Invalid or missing EmailSender NetLogic");
            return;
        }

        var emailVariable = emailUserNode.GetVariable("Email");
        if (emailVariable == null) {
            Log.Error("DigitalAlarmWithEmailNotificationLogic", $"Could not find Email variable of {emailUserNode.BrowseName} user");
            return;
        }

        email = emailVariable.Value;

        Log.Info("NonExclusiveRateOfChangeAlarmWithEmailNotificationLogic", $"Sending email to {email}");
        var args = new string[] { email, alarmBrowseName, alarmMessage };
        emailSender.ExecuteMethod("SendEmail", args);
    }

    private IUANode GetAlarmProperty(string propertyName) {
        var property = alarmObject.GetVariable(propertyName);
        if (property == null) {
            Log.Error("NonExclusiveRateOfChangeAlarmWithEmailNotificationLogic", $"Alarm property {propertyName} could not be found");
            return null;
        }

        var propertyValue = (NodeId)property.Value;
        if (propertyValue == NodeId.Empty || propertyValue == null) {
            Log.Error("NonExclusiveRateOfChangeAlarmWithEmailNotificationLogic", $"Invalid or missing value for alarm property {propertyName}");
            return null;
        }

        var pointedNode = InformationModel.Get(propertyValue);
        if (pointedNode == null) {
            Log.Error("NonExclusiveRateOfChangeAlarmWithEmailNotificationLogic", $"Could not resolve alarm property {propertyName}");
            return null;
        }

        return pointedNode;
    }

    private string email;
    private bool previousActiveState;

    private IEventRegistration eventRegistration;
    private IUANode emailUserNode;
    private IUAObject alarmObject;
}
