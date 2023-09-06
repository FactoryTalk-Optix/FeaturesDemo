#region StandardUsing
using FTOptix.UI;
using UAManagedCore;
#endregion

public class AlarmGridLogic : FTOptix.NetLogic.BaseNetLogic {
    public override void Start() {
        alarmsDatagrid = Owner.Children.Get<DataGrid>("AlarmsDataGrid");
        alarmsDatagridModel = alarmsDatagrid.Children.GetVariable("Model");

        affinityId = alarmsDatagrid.Context.AssignAffinityId();
        RegisterObserverOnSessionLocaleIdChanged(alarmsDatagrid.Context);
    }

    public override void Stop() {
        if (localeIdsRegistration != null) {
            localeIdsRegistration.Dispose();
            localeIdsRegistration = null;
        }

        if (localeIdChangedObserver != null)
            localeIdChangedObserver = null;

    }

    public void RegisterObserverOnSessionLocaleIdChanged(IContext context) {
        var currentSessionLocaleIds = context.Sessions.CurrentSessionInfo.SessionObject.Children["ActualLocaleId"];

        localeIdChangedObserver = new CallbackVariableChangeObserver((IUAVariable variable, UAValue newValue, UAValue oldValue, uint[] _, ulong __) => {
            //reset datagrid model variable to trigger locale changed event
            var dynamicLink = alarmsDatagridModel.GetVariable("DynamicLink");
            if (dynamicLink == null)
                return;

            string dynamicLinkValue = dynamicLink.Value;
            dynamicLink.Value = string.Empty;
            dynamicLink.Value = dynamicLinkValue;
        });

        localeIdsRegistration = currentSessionLocaleIds.RegisterEventObserver(
            localeIdChangedObserver, EventType.VariableValueChanged, affinityId);
    }

    IEventRegistration localeIdsRegistration;
    IEventObserver localeIdChangedObserver;
    uint affinityId;
    DataGrid alarmsDatagrid;
    IUAVariable alarmsDatagridModel;
}
