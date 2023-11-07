#region StandardUsing
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.Recipe;
using FTOptix.UI;
using System;
using System.Collections.Generic;
using UAManagedCore;
using Store = FTOptix.Store;
#endregion

public class RecipesEditorSaveButtonLogic : FTOptix.NetLogic.BaseNetLogic {
    public override void Start() {
        // Insert code to be executed when the user-defined logic is started
        AddTranslations();
    }

    public override void Stop() {
        // Insert code to be executed when the user-defined logic is stopped
    }

    [ExportMethod]
    public void CreateOrSaveRecipe(string recipeName, NodeId recipeSchema) {
        try {
            if (String.IsNullOrEmpty(recipeName))
                throw new Exception(GetLocalizedTextString("RecipesEditorRecipeNameEmpty"));

            if (recipeSchema == null || recipeSchema == NodeId.Empty)
                throw new Exception(GetLocalizedTextString("RecipesEditorRecipeSchemaNodeIdEmpty"));

            var schema = GetRecipeSchema(recipeSchema);
            var store = GetRecipeStore(schema);
            var editModel = GetEditModel(schema);

            if (RecipeExistsInStore(store, schema, recipeName)) {
                // Save Recipe
                schema.CopyToStoreRecipe(editModel.NodeId, recipeName, CopyErrorPolicy.BestEffortCopy);
                SetOutputMessage($"{GetLocalizedTextString("RecipesEditorRecipe")} {recipeName} {GetLocalizedTextString("RecipesEditorSaved")}");
                return;
            }

            // Create recipe
            schema.CreateStoreRecipe(recipeName);

            // Save Recipe
            schema.CopyToStoreRecipe(editModel.NodeId, recipeName, CopyErrorPolicy.BestEffortCopy);

            var comboBox = GetComboBox();
            comboBox.Refresh();

            SetOutputMessage($"{GetLocalizedTextString("RecipesEditorRecipe")} {recipeName} {GetLocalizedTextString("RecipesEditorCreatedAndSaved")}");
        } catch (Exception e) {
            Log.Error("CreateOrSaveRecipe", e.Message);
            SetOutputMessage($"{GetLocalizedTextString("RecipesEditorErrorSavingRecipe")} {e.Message}");
        }
    }

    private RecipeSchema GetRecipeSchema(NodeId recipeSchemaNodeId) {
        // Get RecipeSchema node from its NodeId
        var recipeSchemaNode = LogicObject.Context.GetObject(recipeSchemaNodeId);
        if (recipeSchemaNode == null)
            throw new Exception($"{GetLocalizedTextString("RecipesEditorRecipeNotFound")} {recipeSchemaNodeId}");

        // Check that it is actually a RecipeSchema
        var schema = recipeSchemaNode as RecipeSchema;
        if (schema == null)
            throw new Exception($"{recipeSchemaNode.BrowseName} {GetLocalizedTextString("RecipesEditorNotARecipe")}");

        return schema;
    }

    private Store.Store GetRecipeStore(RecipeSchema schema) {
        // Check if the store is set
        if (schema.Store == NodeId.Empty)
            throw new Exception($"{GetLocalizedTextString("RecipesEditorStoreOfSchema")} {schema.BrowseName} {GetLocalizedTextString("RecipesEditorNotSet")}");

        // Get store node
        var storeNode = LogicObject.Context.GetObject(schema.Store);
        if (storeNode == null)
            throw new Exception($"{GetLocalizedTextString("RecipesEditorStore")} {schema.Store} {GetLocalizedTextString("RecipesEditorNotFound")}");

        // Check that it is actually a store
        var store = storeNode as Store.Store;
        if (store == null)
            throw new Exception($"{GetLocalizedTextString("RecipesEditorStore")} {store.BrowseName} {GetLocalizedTextString("RecipesEditorNotAStore")}");

        return store;
    }

    private IUANode GetEditModel(RecipeSchema schema) {
        var editModel = schema.Children["EditModel"];
        if (editModel == null)
            throw new Exception($"{GetLocalizedTextString("RecipesEditorEditModelOfSchema")} {schema.BrowseName} {GetLocalizedTextString("RecipesEditorNotFound")}");

        return editModel;
    }

    private bool RecipeExistsInStore(Store.Store store, RecipeSchema schema, string recipeName) {
        // Perform query on the store in order to check if the recipe already exists
        object[,] resultSet;
        string[] header;
        var tableName = !String.IsNullOrEmpty(schema.TableName) ? schema.TableName : schema.BrowseName;
        store.Query("SELECT * FROM \"" + tableName + "\" WHERE Name LIKE \'" + recipeName + "\'", out header, out resultSet);
        var rowCount = resultSet != null ? resultSet.GetLength(0) : 0;
        return rowCount > 0;
    }

    private ComboBox GetComboBox() {
        // Find ComboBox
        var comboBoxNode = Owner.Owner.Children.Get("RecipesComboBox");
        if (comboBoxNode == null)
            throw new Exception($"{GetLocalizedTextString("RecipesEditorRecipesComboBoxNotFound")}");

        // Check that it is actually a ComboBox
        ComboBox comboBox = comboBoxNode as ComboBox;
        if (comboBox == null)
            throw new Exception($"{comboBoxNode.BrowseName} {GetLocalizedTextString("RecipesEditorNotAComboBox")}");

        return comboBox;
    }

    private void SetOutputMessage(string message) {
        var outputMessageLabelNode = Owner.Owner.Children.Get("OutputMessage");
        if (outputMessageLabelNode == null)
            throw new Exception($"{GetLocalizedTextString("RecipesEditorOutputMessageLabelNotFound")}");

        var outputMessageLogic1 = outputMessageLabelNode.Children.Get<IUAObject>("RecipesEditorOutputMessageLogic");
        outputMessageLogic1.ExecuteMethod("SetOutputMessage", new object[] { message });
    }

    private string GetLocalizedTextString(string textId) {
        var localizedText = new LocalizedText(textId);
        var stringFound = InformationModel.LookupTranslation(localizedText).Text;

        if (string.IsNullOrEmpty(stringFound)) // fallback to en-US
            return InformationModel.LookupTranslation(localizedText, new List<string>() { "en-US" }).Text;

        return stringFound;
    }

    private void AddTranslationIfNeeded(string key, string it, string en) {
        var alreadyExistsTranslation = !string.IsNullOrEmpty(InformationModel.LookupTranslation(new LocalizedText(key)).Text);
        if (alreadyExistsTranslation)
            return;

        var localizedTextEnglish = new LocalizedText(Project.Current.NodeId.NamespaceIndex, key, en, "en-US");
        InformationModel.AddTranslation(localizedTextEnglish);

        var localizedTextItalian = new LocalizedText(Project.Current.NodeId.NamespaceIndex, key, it, "it-IT");
        InformationModel.SetTranslation(localizedTextItalian);
    }

    private void AddTranslations() {
        AddTranslationIfNeeded("RecipesEditorRecipeNameEmpty", "Il nome della ricetta è vuoto", "Recipe name is empty");
        AddTranslationIfNeeded("RecipesEditorRecipeSchemaNodeIdEmpty", "Il nodeId dello schema della ricetta è vuoto", "Recipe schema nodeId is empty");
        AddTranslationIfNeeded("RecipesEditorRecipe", "Ricetta", "Recipe");
        AddTranslationIfNeeded("RecipesEditorSaved", "salvata", "saved");
        AddTranslationIfNeeded("RecipesEditorCreatedAndSaved", "creata e salvata", "created and saved");
        AddTranslationIfNeeded("RecipesEditorErrorSavingRecipe", "Errore nel salvataggio della ricetta:", "Error saving recipe:");
        AddTranslationIfNeeded("RecipesEditorRecipeNotFound", "Ricetta non trovata, nodeId:", "Recipe not found, nodeId:");
        AddTranslationIfNeeded("RecipesEditorNotARecipe", "non è una ricetta", "is not a recipe");
        AddTranslationIfNeeded("RecipesEditorStoreOfSchema", "Lo store dello schema", "Store of schema");
        AddTranslationIfNeeded("RecipesEditorNotSet", "non è impostato", "is not set");
        AddTranslationIfNeeded("RecipesEditorStore", "Store", "Store");
        AddTranslationIfNeeded("RecipesEditorNotFound", "non trovato", "not found");
        AddTranslationIfNeeded("RecipesEditorNotAStore", "non è uno store", "is not a store");
        AddTranslationIfNeeded("RecipesEditorEditModelOfSchema", "EditModel dello schema", "EditModel of schema");
        AddTranslationIfNeeded("RecipesEditorRecipesComboBoxNotFound", "Nodo RecipesComboBox non trovato", "RecipesComboBox node not found");
        AddTranslationIfNeeded("RecipesEditorNotAComboBox", "non è una comboBox", "is not a comboBox");
        AddTranslationIfNeeded("RecipesEditorOutputMessageLabelNotFound", "Etichetta OutputMessage non trovata", "OutputMessage label not found");
    }
}
