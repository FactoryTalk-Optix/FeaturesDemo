#region Using directives
using FTOptix.NetLogic;
using System;
using System.Linq;
using UAManagedCore;
using FTOptix.UI;
#endregion

public class AlarmBannerLogic : BaseNetLogic {
    public override void Start() {
        var context = LogicObject.Context;

        affinityId = context.AssignAffinityId();

        RegisterObserverOnLocalizedAlarmsContainer(context);
        RegisterObserverOnSessionActualLanguagesChange(context);
        RegisterObserverOnLocalizedAlarmsObject(context);
    }

    public override void Stop() {
        alarmEventRegistration?.Dispose();
        alarmEventRegistration2?.Dispose();
        sessionActualLanguagesRegistration?.Dispose();
        alarmBannerSelector?.Dispose();

        alarmEventRegistration = null;
        alarmEventRegistration2 = null;
        sessionActualLanguagesRegistration = null;
        alarmBannerSelector = null;

        alarmsNotificationObserver = null;
        retainedAlarmsObjectObserver = null;
    }

    [ExportMethod]
    public void NextAlarm() {
        alarmBannerSelector?.OnNextAlarmClicked();
    }

    [ExportMethod]
    public void PreviousAlarm() {
        alarmBannerSelector?.OnPreviousAlarmClicked();
    }

    public void RegisterObserverOnLocalizedAlarmsObject(IContext context) {
        var retainedAlarms = context.GetNode(FTOptix.Alarm.Objects.RetainedAlarms);

        retainedAlarmsObjectObserver = new RetainedAlarmsObjectObserver((ctx) => RegisterObserverOnLocalizedAlarmsContainer(ctx));

        // observe ReferenceAdded of localized alarm containers
        alarmEventRegistration2 = retainedAlarms.RegisterEventObserver(
            retainedAlarmsObjectObserver, EventType.ForwardReferenceAdded, affinityId);
    }

    public void RegisterObserverOnLocalizedAlarmsContainer(IContext context) {
        var retainedAlarms = context.GetNode(FTOptix.Alarm.Objects.RetainedAlarms);
        var localizedAlarmsVariable = retainedAlarms.GetVariable("LocalizedAlarms");
        var localizedAlarmsNodeId = (NodeId)localizedAlarmsVariable.Value;
        IUANode localizedAlarmsContainer = null;
        if (localizedAlarmsNodeId != null && !localizedAlarmsNodeId.IsEmpty)
            localizedAlarmsContainer = context.GetNode(localizedAlarmsNodeId);

        if (alarmEventRegistration != null) {
            alarmEventRegistration.Dispose();
            alarmEventRegistration = null;
        }

        if (alarmBannerSelector != null)
            alarmBannerSelector.Dispose();
        alarmBannerSelector = new AlarmBannerSelector(LogicObject, localizedAlarmsContainer);

        alarmsNotificationObserver = new AlarmsNotificationObserver(LogicObject, localizedAlarmsContainer, alarmBannerSelector);
        alarmsNotificationObserver.Initialize();

        alarmEventRegistration = localizedAlarmsContainer?.RegisterEventObserver(
            alarmsNotificationObserver,
            EventType.ForwardReferenceAdded | EventType.ForwardReferenceRemoved, affinityId);
    }

    public void RegisterObserverOnSessionActualLanguagesChange(IContext context) {
        var currentSessionActualLanguages = context.Sessions.CurrentSessionInfo.SessionObject.Children["ActualLanguage"];

        sessionActualLanguagesChangeObserver = new CallbackVariableChangeObserver(
            (IUAVariable variable, UAValue newValue, UAValue oldValue, uint[] indexes, ulong senderId) => {
                RegisterObserverOnLocalizedAlarmsContainer(context);
            });

        sessionActualLanguagesRegistration = currentSessionActualLanguages.RegisterEventObserver(
            sessionActualLanguagesChangeObserver, EventType.VariableValueChanged, affinityId);
    }

    private class RetainedAlarmsObjectObserver : IReferenceObserver {
        public RetainedAlarmsObjectObserver(Action<IContext> action) {
            registrationCallback = action;
        }

        public void OnReferenceAdded(IUANode sourceNode, IUANode targetNode, NodeId referenceTypeId, ulong senderId) {
            string localeId = "en-US";

            var localeIds = targetNode.Context.Sessions.CurrentSessionHandler.ActualLocaleId;
            if (!String.IsNullOrEmpty(localeIds))
                localeId = localeIds;

            targetNode.Context.Sessions.CurrentSessionHandler.ActualLocaleId.First();

            if (targetNode.BrowseName == localeId)
                registrationCallback(targetNode.Context);
        }

        public void OnReferenceRemoved(IUANode sourceNode, IUANode targetNode, NodeId referenceTypeId, ulong senderId) {
        }

        private Action<IContext> registrationCallback;
    }

    uint affinityId = 0;
    AlarmsNotificationObserver alarmsNotificationObserver;
    RetainedAlarmsObjectObserver retainedAlarmsObjectObserver;
    IEventRegistration alarmEventRegistration;
    IEventRegistration alarmEventRegistration2;
    IEventRegistration sessionActualLanguagesRegistration;
    IEventObserver sessionActualLanguagesChangeObserver;
    AlarmBannerSelector alarmBannerSelector;

    public class AlarmsNotificationObserver : IReferenceObserver {
        public AlarmsNotificationObserver(IUANode logicNode, IUANode localizedAlarmsContainer, AlarmBannerSelector alarmBannerSelector) {
            this.logicNode = logicNode;
            this.alarmBannerSelector = alarmBannerSelector;
            this.localizedAlarmsContainer = localizedAlarmsContainer;
        }

        public void Initialize() {
            retainedAlarmsCount = logicNode.GetVariable("AlarmCount");

            var count = localizedAlarmsContainer?.Children.Count ?? 0;
            retainedAlarmsCount.Value = count;
            if (alarmBannerSelector != null && count > 0)
                alarmBannerSelector.Initialize();
        }

        public void OnReferenceAdded(IUANode sourceNode, IUANode targetNode, NodeId referenceTypeId, ulong senderId) {
            retainedAlarmsCount.Value = localizedAlarmsContainer?.Children.Count ?? 0;
            if (alarmBannerSelector != null && !alarmBannerSelector.RotationRunning)
                alarmBannerSelector.Initialize();
        }

        public void OnReferenceRemoved(IUANode sourceNode, IUANode targetNode, NodeId referenceTypeId, ulong senderId) {
            var count = localizedAlarmsContainer?.Children.Count ?? 0;
            retainedAlarmsCount.Value = count;
            if (alarmBannerSelector == null)
                return;

            if (count == 0)
                alarmBannerSelector.Reset();
            else if (alarmBannerSelector.CurrentDisplayedAlarmNodeId == targetNode.NodeId)
                alarmBannerSelector.Initialize();
        }

        private IUAVariable retainedAlarmsCount;

        private IUANode localizedAlarmsContainer;
        private IUANode logicNode;
        private AlarmBannerSelector alarmBannerSelector;
    }
}

public class AlarmBannerSelector : IDisposable {
    public AlarmBannerSelector(IUANode logicNode, IUANode localizedAlarmsContainer) {
        this.logicNode = logicNode;
        this.localizedAlarmsContainer = localizedAlarmsContainer;

        currentDisplayedAlarm = logicNode.GetVariable("CurrentDisplayedAlarm");
        currentDisplayedAlarmIndex = logicNode.GetVariable("CurrentDisplayedAlarmIndex");

        rotationTime = logicNode.GetVariable("RotationTime");
        rotationTime.VariableChange += RotationTime_VariableChange;

        rotationTask = new PeriodicTask(DisplayNextAlarm, rotationTime.Value, logicNode);
    }

    private void RotationTime_VariableChange(object sender, VariableChangeEventArgs e) {
        var wasRunning = RotationRunning;
        StopRotation();
        rotationTask = new PeriodicTask(DisplayNextAlarm, e.NewValue, logicNode);
        if (wasRunning)
            StartRotation();
    }

    public void Initialize() {
        ChangeCurrentAlarm(0);
        if (RotationRunning)
            StopRotation();
        StartRotation();
    }

    public void Reset() {
        ChangeCurrentAlarm(0);
        StopRotation();
    }

    public bool RotationRunning { get; private set; }
    public NodeId CurrentDisplayedAlarmNodeId {
        get { return currentDisplayedAlarm.Value; }
    }

    public void OnNextAlarmClicked() {
        RestartRotation();
        DisplayNextAlarm();
    }

    public void OnPreviousAlarmClicked() {
        RestartRotation();
        DisplayPreviousAlarm();
    }

    private void StopRotation() {
        if (!RotationRunning)
            return;

        rotationTask.Cancel();
        RotationRunning = false;
        skipFirstCallBack = false;
    }

    private void StartRotation() {
        if (RotationRunning)
            return;

        rotationTask.Start();
        RotationRunning = true;
        skipFirstCallBack = true;
    }

    private void RestartRotation() {
        StopRotation();
        StartRotation();
    }

    private void DisplayPreviousAlarm() {
        var index = currentDisplayedAlarmIndex.Value;
        var size = localizedAlarmsContainer?.Children.Count ?? 0;
        var previousIndex = index - 1 < 0 ? size - 1 : index - 1;

        ChangeCurrentAlarm(previousIndex);
    }

    private void DisplayNextAlarm() {
        if (skipFirstCallBack) {
            skipFirstCallBack = false;
            return;
        }

        var index = currentDisplayedAlarmIndex.Value;
        var size = localizedAlarmsContainer?.Children.Count ?? 0;
        var nextIndex = index + 1 == size ? 0 : index + 1;

        ChangeCurrentAlarm(nextIndex);
    }

    private void ChangeCurrentAlarm(int index) {
        var size = localizedAlarmsContainer?.Children.Count ?? 0;
        if (size == 0) {
            currentDisplayedAlarm.Value = NodeId.Empty;
            currentDisplayedAlarmIndex.Value = 0;
            return;
        }

        currentDisplayedAlarmIndex.Value = index;
        try {
            var alarmToDisplay = localizedAlarmsContainer.Children.ElementAt(index);
            if (alarmToDisplay != null)
                currentDisplayedAlarm.Value = alarmToDisplay.NodeId;
        } catch (Exception) {
            currentDisplayedAlarm.Value = NodeId.Empty;
            currentDisplayedAlarmIndex.Value = 0;
        }
    }

    private PeriodicTask rotationTask;
    private IUANode localizedAlarmsContainer;
    private IUAVariable currentDisplayedAlarm;
    private IUAVariable currentDisplayedAlarmIndex;
    private IUAVariable rotationTime;
    private IUANode logicNode;
    private bool skipFirstCallBack = false;

    #region IDisposable Support
    private bool disposedValue = false;

    protected virtual void Dispose(bool disposing) {
        if (disposedValue)
            return;

        if (disposing) {
            Reset();
            rotationTask.Dispose();
        }

        disposedValue = true;
    }

    public void Dispose() {
        Dispose(true);
    }
    #endregion
}
