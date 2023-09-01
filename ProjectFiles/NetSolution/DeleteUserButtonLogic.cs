#region Using directives
using System;
using FTOptix.CoreBase;
using FTOptix.HMIProject;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.NetLogic;
using FTOptix.OPCUAServer;
using FTOptix.UI;
#endregion

public class DeleteUserButtonLogic : BaseNetLogic
{
    [ExportMethod]
    public void DeleteUser(NodeId userToDelete)
    {
        var userObjectToRemove = InformationModel.Get(userToDelete);
        if (userObjectToRemove == null)
        {
            Log.Error("UserEditor", "Cannot obtain the selected user.");
            return;
        }

        var userVariable = Owner.Owner.Owner.Owner.GetVariable("Users");
        if (userVariable == null)
        {
            Log.Error("UserEditor", "Missing user variable in UserEditor Panel.");
            return;
        }

        if (userVariable.Value == null || (NodeId)userVariable.Value == NodeId.Empty)
        {
            Log.Error("UserEditor", "Fill User variable in UserEditor.");
            return;
        }
        var userFolder = InformationModel.Get(userVariable.Value);
        if (userFolder == null)
        {
            Log.Error("UserEditor", "Cannot obtain Users folder.");
            return;
        }

        userFolder.Remove(userObjectToRemove);
    }
}
