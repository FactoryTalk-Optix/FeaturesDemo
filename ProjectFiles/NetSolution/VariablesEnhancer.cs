#region Using directives
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using UAManagedCore;
using FTOptix.UI;
#endregion

public class VariablesEnhancer : BaseNetLogic {
    public override void Start() {
        enhancer();
    }

    public override void Stop() {
        // Insert code to be executed when the user-defined logic is stopped
    }

    private void enhancer() {
        // Initialize variable XYChartArray used in Converters Section
        var myArray = Project.Current.GetVariable("Model/Converters/EngineeringUnit/XYChartArray");
        int[,] myValues = new int[2, 2];

        myValues[0, 0] = 0;     // x0
        myValues[1, 0] = 1024;   // y0

        myValues[0, 1] = 0;
        myValues[1, 1] = 100;

        myArray.Value = new UAValue(myValues);

        // Initialize variable XYChartArray used in Converters Section
        myArray = Project.Current.GetVariable("Model/Data/XYChart/PointArray");
        myValues = new int[10, 2];

        myValues[0, 0] = 52;
        myValues[0, 1] = -96;

        myValues[1, 0] = 161;
        myValues[1, 1] = -135;

        myValues[2, 0] = 188;
        myValues[2, 1] = 155;

        myValues[3, 0] = 52;
        myValues[3, 1] = 155;

        myValues[4, 0] = -148;
        myValues[4, 1] = 32;

        myValues[5, 0] = 92;
        myValues[5, 1] = 27;

        myValues[6, 0] = -102;
        myValues[6, 1] = -95;

        myValues[7, 0] = -259;
        myValues[7, 1] = -71;

        myValues[8, 0] = -263;
        myValues[8, 1] = 125;

        myValues[9, 0] = -134;
        myValues[9, 1] = 137;

        myArray.Value = new UAValue(myValues);
    }

}
