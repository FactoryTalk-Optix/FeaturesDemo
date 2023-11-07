#region Using directives
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using System;
using System.Collections.Generic;
using System.Reflection;
using UAManagedCore;
using FTOptix.UI;
#endregion

public class VectorEditorUpdater : BaseNetLogic {
    public override void Start() {
        var context = LogicObject.Context;
        logicObjectAffinityId = context.AssignAffinityId();
        logicObjectSenderId = context.AssignSenderId();

        // Check if the given array is valid and convert it to a C# Array
        vectorValueVariable = Owner.GetVariable("VectorValue");
        if (vectorValueVariable == null)
            throw new CoreConfigurationException("Unable to find VectorValue variable");
        var vectorValueVariableValue = vectorValueVariable.Value.Value;
        if (!vectorValueVariableValue.GetType().IsArray)
            throw new CoreConfigurationException("VectorValue is not an array");
        var vectorArray = (Array)vectorValueVariableValue;
        if (vectorArray.Rank != 1)
            throw new CoreConfigurationException("Only one-dimensional arrays are supported");

        // GridModel represents a support variable that acts as a link between the VectorValue model variable and the widget data grid.
        gridModelVariable = LogicObject.GetVariable("GridModel");

        using (var resumeDispatchOnExit = context.SuspendDispatch(logicObjectAffinityId)) {
            // Register the observer on VectorValue
            vectorValueVariableChangeObserver = new CallbackVariableChangeObserver(VectorValueVariableValueChanged);
            vectorValueVariableRegistration = vectorValueVariable.RegisterEventObserver(
                vectorValueVariableChangeObserver, EventType.VariableValueChanged, logicObjectAffinityId);

            cellVariableChangeObserver = new CallbackVariableChangeObserver(CellVariableValueChanged);
            CreateGrid(vectorArray);
        }
    }

    public override void Stop() {
        using (var destroyDispatchOnExit = LogicObject.Context.TerminateDispatchOnStop(logicObjectAffinityId)) {
            if (cellVariableRegistrations != null) {
                cellVariableRegistrations.ForEach(registration => registration.Dispose());
                cellVariableRegistrations = null;
            }

            if (vectorValueVariableRegistration != null) {
                vectorValueVariableRegistration.Dispose();
                vectorValueVariableRegistration = null;
            }

            if (gridModelVariable != null)
                gridModelVariable.Value = NodeId.Empty;

            if (gridObject != null) {
                gridObject.Delete();
                gridObject = null;
            }

            currentRowCount = 0;

            gridModelVariable = null;
            vectorValueVariable = null;
            logicObjectSenderId = 0;
            logicObjectAffinityId = 0;
        }
    }

    #region Initialize GridModel from VectorValue
    private void CreateGrid(Array vectorArray) {
        cellVariableRegistrations = new List<IEventRegistration>();

        currentRowCount = (uint)vectorArray.GetLength(0);

        // Create and initialize the Grid-supporting object
        gridObject = InformationModel.MakeObject("Grid");
        for (uint rowIndex = 0; rowIndex < currentRowCount; ++rowIndex)
            gridObject.Add(CreateRow(vectorArray, rowIndex));

        LogicObject.Add(gridObject);
        gridModelVariable.Value = gridObject.NodeId;
    }

    private IUAObject CreateRow(Array vectorArray, uint rowIndex) {
        var rowObject = InformationModel.MakeObject($"Row{rowIndex}");

        // Determine the OPC UA type from the given C# Array
        var netType = vectorArray.GetType().GetElementType().GetTypeInfo();
        var opcuaTypeNodeId = DataTypesHelper.GetDataTypeIdByNetType(netType);
        if (opcuaTypeNodeId == null)
            throw new CoreConfigurationException($"Unable to find an OPC UA data type corresponding to the {netType} .NET type");

        // Create the cell variable and register for changes
        var cellVariable = InformationModel.MakeVariable("Cell0", opcuaTypeNodeId);
        cellVariable.Value = new UAValue(vectorArray.GetValue(rowIndex));
        cellVariableRegistrations.Add(cellVariable.RegisterEventObserver(cellVariableChangeObserver,
            EventType.VariableValueChanged, logicObjectAffinityId));

        // Add the cell variable to the grid
        rowObject.Add(cellVariable);

        return rowObject;
    }

    #endregion

    #region Monitor each element inside VectorValue
    private void CellVariableValueChanged(IUAVariable variable, UAValue newValue, UAValue oldValue, uint[] indexes, ulong senderId) {
        if (senderId == logicObjectSenderId)
            return;

        var rowBrowseName = variable.Owner.BrowseName;
        var rowIndex = uint.Parse(rowBrowseName.Remove(0, "Row".Length));

        using (var restorePreviousSenderIdOnExit = LogicObject.Context.SetCurrentThreadSenderId(logicObjectSenderId)) {
            vectorValueVariable.SetValue(newValue.Value, new uint[] { rowIndex });
        }
    }

    #endregion

    #region Monitor VectorValue variable
    private void VectorValueVariableValueChanged(IUAVariable variable, UAValue newValue, UAValue oldValue, uint[] indexes, ulong senderId) {
        if (senderId == logicObjectSenderId)
            return;

        if (indexes.Length > 0)
            UpdateCellValue(newValue, indexes);
        else
            UpdateAllCellValues((Array)newValue.Value);
    }

    private void UpdateAllCellValues(Array vectorArray) {
        var rowCount = (uint)vectorArray.GetLength(0);

        // Add or remove rows if the number of rows changes
        if (rowCount > currentRowCount)
            AddRows(currentRowCount, rowCount - 1, vectorArray);
        else if (rowCount < currentRowCount)
            RemoveLastRows(rowCount, currentRowCount - 1);

        currentRowCount = rowCount;

        for (uint rowIndex = 0; rowIndex < rowCount; ++rowIndex)
            UpdateCellValue(new UAValue(vectorArray.GetValue(rowIndex)), new uint[] { rowIndex });
    }

    private void AddRows(uint fromRow, uint toRow, Array values) {
        for (uint rowIndex = fromRow; rowIndex <= toRow; ++rowIndex)
            gridObject.Add(CreateRow(values, rowIndex));
    }

    private void RemoveLastRows(uint fromRow, uint toRow) {
        for (uint rowIndex = fromRow; rowIndex <= toRow; ++rowIndex) {
            var rowObject = gridObject.Children[$"Row{rowIndex}"];
            rowObject.Delete();
        }
    }

    private void UpdateCellValue(UAValue newValue, uint[] indexes) {
        var cellObject = gridObject.Children[$"Row{indexes[0]}"].GetVariable("Cell0");

        using (var restorePreviousSenderIdOnExit = LogicObject.Context.SetCurrentThreadSenderId(logicObjectSenderId)) {
            cellObject.Value = newValue;
        }
    }

    #endregion

    private uint logicObjectAffinityId;
    private ulong logicObjectSenderId;

    private IUAVariable vectorValueVariable;
    private IUAVariable gridModelVariable;
    private IUAObject gridObject;
    private uint currentRowCount = 0;

    private IEventObserver vectorValueVariableChangeObserver;
    private IEventObserver cellVariableChangeObserver;
    private IEventRegistration vectorValueVariableRegistration;
    private List<IEventRegistration> cellVariableRegistrations;
}
