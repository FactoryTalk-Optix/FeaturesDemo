#region Using directives
using FTOptix.NetLogic;
using FTOptix.UI;
using UAManagedCore;
#endregion

public class AutoRefresher : BaseNetLogic {
    FTOptix.UI.DataGrid dataGrid;
    public override void Start() {
        var autoRefreshCheckBox = LogicObject.Owner.Owner.Get<CheckBox>("ValueAutoRefresh");
        var activeVariable = autoRefreshCheckBox.CheckedVariable;
        activeVariable.VariableChange += OnActiveVariableChanged;
    }

    private void OnActiveVariableChanged(object sender, VariableChangeEventArgs e) {
        if ((bool)e.NewValue) {
            dataGrid = (FTOptix.UI.DataGrid)Owner;
            refreshTask = new PeriodicTask(RefreshDataGrid, 1100, LogicObject);
            refreshTask.Start();
        } else {
            refreshTask?.Dispose();
        }
    }

    public override void Stop() {
        refreshTask?.Dispose();
    }

    public void RefreshDataGrid() {
        dataGrid.Refresh();
    }

    private PeriodicTask refreshTask;
}
