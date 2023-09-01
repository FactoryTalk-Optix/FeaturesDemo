#region Using directives
using FTOptix.Core;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using UAManagedCore;
using FTOptix.UI;
#endregion

public class CreateUiObjectRuntime : BaseNetLogic {
    public override void Start() {
        // Insert code to be executed when the user-defined logic is started
    }

    public override void Stop() {
        // Insert code to be executed when the user-defined logic is stopped
    }

    [FTOptix.NetLogic.ExportMethod]
    public void Configuration() {
        // Read number of FAN to be set
        var numberFanExisting = Project.Current.GetObject("RuntimeObjectUI").Children.Count;

        // Read variable indicating the number of FAN
        var numberFun = Project.Current.GetObject("Model/PrototypesInstances/RuntimeUI").Children.Get<IUAVariable>("NumberOfInstances").Value;

        // Execute for the number of FANs to create
        if (numberFun - numberFanExisting > 0) {
            for (int i = numberFanExisting + 1; i <= numberFun; ++i) {
                // Create object of type FAN and insert in the appropriate folder
                var modelloFan = InformationModel.MakeObject<Fan>("Fan number" + i);
                modelloFan.Number = i;
                Project.Current.Get<Folder>("RuntimeObjectUI").Children.Add(modelloFan);

                // Create instances of PANEL FAN and insert them in "HorizontalLayout" container
                var panelFan = InformationModel.MakeObject<RuntimeUIInstance>("Fan Panel" + i);
                panelFan.TopMargin = 20;
                panelFan.LeftMargin = 20;
                Project.Current.Get("UI/Sections/PrototypesInstances/RuntimeUIInstanceContainer/ScrollView/HorizontalLayout").Children.Add(panelFan);

                // Enhance alias
                panelFan.SetAlias("FanAlias", Project.Current.Get("RuntimeObjectUI/Fan number" + i));
            }
        } else if (numberFun - numberFanExisting < 0) {
            for (int i = numberFanExisting; i > numberFun; --i) {
                // Search object of type FAN and delete from the appropriate folder
                var modelloFan = Project.Current.Get("RuntimeObjectUI/Fan number" + i);
                Project.Current.Get("RuntimeObjectUI").Remove(modelloFan);

                // Search for instances of PANEL FAN and delete from "HorizontalLayout" object
                var panelFan = Project.Current.Get("UI/Sections/PrototypesInstances/RuntimeUIInstanceContainer/ScrollView/HorizontalLayout/Fan Panel" + i);
                Project.Current.Get("UI/Sections/PrototypesInstances/RuntimeUIInstanceContainer/ScrollView/HorizontalLayout").Remove(panelFan);
            }
        }
    }

    [FTOptix.NetLogic.ExportMethod]
    public void EreaseFan() {
        Project.Current.GetObject("RuntimeObjectUI").Children.Clear();
        Project.Current.Get("UI/Sections/PrototypesInstances/RuntimeUIInstanceContainer/ScrollView/HorizontalLayout").Children.Clear();
    }

}


