#region StandardUsing
using FTOptix.Core;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UAManagedCore;
using FTOptix.UI;
using OpcUa = UAManagedCore.OpcUa;
#endregion

public class ImportAndExportTranslations : BaseNetLogic {
    [ExportMethod]
    public void ExportTranslations() {
        var csvPath = GetCSVFilePath();
        if (string.IsNullOrEmpty(csvPath)) {
            Log.Error("ImportAndExportTranslations", "No CSV file, please fill the CSVPath variable");
            return;
        }

        char? characterSeparator = GetCharacterSeparator();
        if (characterSeparator == null || characterSeparator == '\0')
            return;

        bool wrapFields = GetWrapFields();

        var localizationDictionary = GetDictionary();
        if (localizationDictionary == null) {
            Log.Error("ImportAndExportTranslations", "No translation table found");
            return;
        }

        var dictionary = (string[,])localizationDictionary.Value.Value;
        var rowCount = dictionary.GetLength(0);
        var columnCount = dictionary.GetLength(1);

        try {
            using (var csvWriter = new CSVFileWriter(csvPath) { FieldDelimiter = characterSeparator.Value, WrapFields = wrapFields }) {
                for (var r = 0; r < rowCount; ++r) {
                    var row = new string[columnCount];

                    for (var c = 0; c < columnCount; ++c)
                        row[c] = dictionary[r, c];

                    csvWriter.WriteLine(row);
                }
            }

            Log.Info("ImportAndExportTranslations", $"Translations successfully exported to {csvPath}");
        } catch (Exception ex) {
            Log.Error("ImportAndExportTranslations", $"Unable to export the translations: {ex}");
        }
    }

    [ExportMethod]
    public void ImportTranslations() {
        var csvPath = GetCSVFilePath();

        if (string.IsNullOrEmpty(csvPath)) {
            Log.Error("ImportAndExportTranslations", "No CSV file chosen, please fill the CSVPath variable");
            return;
        }

        char? characterSeparator = GetCharacterSeparator();
        if (characterSeparator == null || characterSeparator == '\0')
            return;

        bool wrapFields = GetWrapFields();

        var localizationDictionary = GetDictionary();
        if (localizationDictionary == null) {
            Log.Error("ImportAndExportTranslations", "No translation table found");
            return;
        }

        if (!File.Exists(csvPath)) {
            Log.Error("ImportAndExportTranslations", $"The file {csvPath} does not exist");
            return;
        }

        try {
            using (var csvReader = new CSVFileReader(csvPath) { FieldDelimiter = characterSeparator.Value, WrapFields = wrapFields }) {

                if (csvReader.EndOfFile()) {
                    Log.Error("ImportAndExportTranslations", $"The file {csvPath} is empty");
                    return;
                }

                var fileLines = csvReader.ReadAll();
                if (fileLines.Count == 0 || fileLines[0].Count == 0)
                    return;

                int numColumns = fileLines[0].Count;

                var importedTranslations = new string[fileLines.Count, numColumns];

                for (var r = 0; r < fileLines.Count; ++r)
                    for (var c = 0; c < fileLines[r].Count; ++c)
                        importedTranslations[r, c] = fileLines[r][c];

                localizationDictionary.Value = new UAValue(importedTranslations);
            }

            Log.Info("ImportAndExportTranslations", $"Translations successfully imported into {localizationDictionary.BrowseName} localization dictionary");
        } catch (Exception ex) {
            Log.Error("ImportAndExportTranslations", $"Unable to import the translations: {ex}");
        }
    }

    private string GetCSVFilePath() {
        var csvPathVariable = LogicObject.Children.GetVariable("CSVPath");
        if (csvPathVariable == null) {
            Log.Error("ImportAndExportTranslations", "CSVPath variable not found");
            return "";
        }

        return new ResourceUri(csvPathVariable.Value).Uri;
    }

    private char? GetCharacterSeparator() {
        var separatorVariable = LogicObject.GetVariable("CharacterSeparator");
        if (separatorVariable == null) {
            Log.Error("ImportAndExportTranslations", "CharacterSeparator variable not found");
            return null;
        }

        string separator = separatorVariable.Value;

        if (separator.Length != 1 || separator == string.Empty) {
            Log.Error("ImportAndExportTranslations", "Wrong CharacterSeparator configuration. Please insert a char");
            return null;
        }

        if (char.TryParse(separator, out char result))
            return result;

        return null;
    }

    private bool GetWrapFields() {
        var wrapFieldsVariable = LogicObject.GetVariable("WrapFields");
        if (wrapFieldsVariable == null) {
            Log.Error("ImportAndExportTranslations", "WrapFields variable not found");
            return false;
        }

        return wrapFieldsVariable.Value;
    }

    private IUAVariable GetDictionary() {
        var dictionaryVariable = LogicObject.GetVariable("LocalizationDictionary");
        if (dictionaryVariable == null) {
            Log.Info("ImportAndExportTranslations", "The first localization dictionary found will be used since the LocalizationDictionary variable cannot be not found");
            return GetDefaultDictionary();
        }

        NodeId nodeIdDictionaryValue = dictionaryVariable.Value;
        if (nodeIdDictionaryValue == null) {
            Log.Info("ImportAndExportTranslations", "The first localization dictionary found will be used since the LocalizationDictionary variable is not set");
            return GetDefaultDictionary();
        }

        var dictionaryNode = LogicObject.Context.GetNode(nodeIdDictionaryValue);
        if (dictionaryNode == null) {
            Log.Error("ImportAndExportTranslations", "The node pointed by the LocalizationDictionary variable cannot be found");
            return null;
        }

        var resultDictionaryVariable = dictionaryNode as IUAVariable;
        if (resultDictionaryVariable == null || !resultDictionaryVariable.IsInstanceOf(FTOptix.Core.VariableTypes.LocalizationDictionary))
            Log.Error("The node pointed by the LocalizationDictionary variable is not a localization dictionary");

        return resultDictionaryVariable;
    }

    private IUAVariable GetDefaultDictionary() {
        var localizationDictionaryType = Project.Current.Context.GetNode(FTOptix.Core.VariableTypes.LocalizationDictionary);
        var localizationDictionaries = localizationDictionaryType.InverseRefs.GetNodes(OpcUa.ReferenceTypes.HasTypeDefinition);

        foreach (var dictionaryNode in localizationDictionaries) {
            if (dictionaryNode.NodeId.NamespaceIndex == Project.Current.NodeId.NamespaceIndex)
                return (IUAVariable)dictionaryNode;
        }

        return null;
    }

    #region CSV Read/Write classes
    private class CSVFileReader : IDisposable {
        public char FieldDelimiter { get; set; } = ',';

        public char QuoteChar { get; set; } = '"';

        public bool WrapFields { get; set; } = false;

        public bool IgnoreMalformedLines { get; set; } = false;

        public CSVFileReader(string filePath, System.Text.Encoding encoding) {
            streamReader = new StreamReader(filePath, encoding);
        }

        public CSVFileReader(string filePath) {
            streamReader = new StreamReader(filePath, System.Text.Encoding.UTF8);
        }

        public CSVFileReader(StreamReader streamReader) {
            this.streamReader = streamReader;
        }

        public bool EndOfFile() {
            return streamReader.EndOfStream;
        }

        public List<string> ReadLine() {
            if (EndOfFile())
                return null;

            var line = streamReader.ReadLine();

            var result = WrapFields ? ParseLineWrappingFields(line) : ParseLineWithoutWrappingFields(line);

            currentLineNumber++;
            return result;

        }

        public List<List<string>> ReadAll() {
            var result = new List<List<string>>();
            while (!EndOfFile())
                result.Add(ReadLine());

            return result;
        }

        private List<string> ParseLineWithoutWrappingFields(string line) {
            if (string.IsNullOrEmpty(line) && !IgnoreMalformedLines)
                throw new FormatException($"Error processing line {currentLineNumber}. Line cannot be empty");

            return line.Split(FieldDelimiter).ToList();
        }

        private List<string> ParseLineWrappingFields(string line) {
            var fields = new List<string>();
            var buffer = new StringBuilder("");
            var fieldParsing = false;

            int i = 0;
            while (i < line.Length) {
                if (!fieldParsing) {
                    if (IsWhiteSpace(line, i)) {
                        ++i;
                        continue;
                    }

                    // Line and column numbers must be 1-based for messages to user
                    var lineErrorMessage = $"Error processing line {currentLineNumber}";
                    if (i == 0) {
                        // A line must begin with the quotation mark
                        if (!IsQuoteChar(line, i)) {
                            if (IgnoreMalformedLines)
                                return null;
                            else
                                throw new FormatException($"{lineErrorMessage}. Expected quotation marks at column {i + 1}");
                        }

                        fieldParsing = true;
                    } else {
                        if (IsQuoteChar(line, i))
                            fieldParsing = true;
                        else if (!IsFieldDelimiter(line, i)) {
                            if (IgnoreMalformedLines)
                                return null;
                            else
                                throw new FormatException($"{lineErrorMessage}. Wrong field delimiter at column {i + 1}");
                        }
                    }

                    ++i;
                } else {
                    if (IsEscapedQuoteChar(line, i)) {
                        i += 2;
                        buffer.Append(QuoteChar);
                    } else if (IsQuoteChar(line, i)) {
                        fields.Add(buffer.ToString());
                        buffer.Clear();
                        fieldParsing = false;
                        ++i;
                    } else {
                        buffer.Append(line[i]);
                        ++i;
                    }
                }
            }

            return fields;
        }

        private bool IsEscapedQuoteChar(string line, int i) {
            return line[i] == QuoteChar && i != line.Length - 1 && line[i + 1] == QuoteChar;
        }

        private bool IsQuoteChar(string line, int i) {
            return line[i] == QuoteChar;
        }

        private bool IsFieldDelimiter(string line, int i) {
            return line[i] == FieldDelimiter;
        }

        private bool IsWhiteSpace(string line, int i) {
            return Char.IsWhiteSpace(line[i]);
        }

        private StreamReader streamReader;
        private int currentLineNumber = 1;

        #region IDisposable support
        private bool disposed = false;
        protected virtual void Dispose(bool disposing) {
            if (disposed)
                return;

            if (disposing)
                streamReader.Dispose();

            disposed = true;
        }

        public void Dispose() {
            Dispose(true);
        }
        #endregion
    }

    private class CSVFileWriter : IDisposable {
        public char FieldDelimiter { get; set; } = ',';

        public char QuoteChar { get; set; } = '"';

        public bool WrapFields { get; set; } = false;

        public CSVFileWriter(string filePath) {
            streamWriter = new StreamWriter(filePath, false, System.Text.Encoding.UTF8);
        }

        public CSVFileWriter(string filePath, System.Text.Encoding encoding) {
            streamWriter = new StreamWriter(filePath, false, encoding);
        }

        public CSVFileWriter(StreamWriter streamWriter) {
            this.streamWriter = streamWriter;
        }

        public void WriteLine(string[] fields) {
            var stringBuilder = new StringBuilder();

            for (var i = 0; i < fields.Length; ++i) {
                if (WrapFields)
                    stringBuilder.AppendFormat("{0}{1}{0}", QuoteChar, EscapeField(fields[i]));
                else
                    stringBuilder.AppendFormat("{0}", fields[i]);

                if (i != fields.Length - 1)
                    stringBuilder.Append(FieldDelimiter);
            }

            streamWriter.WriteLine(stringBuilder.ToString());
            streamWriter.Flush();
        }

        private string EscapeField(string field) {
            var quoteCharString = QuoteChar.ToString();
            return field.Replace(quoteCharString, quoteCharString + quoteCharString);
        }

        private StreamWriter streamWriter;

        #region IDisposable Support
        private bool disposed = false;
        protected virtual void Dispose(bool disposing) {
            if (disposed)
                return;

            if (disposing)
                streamWriter.Dispose();

            disposed = true;
        }

        public void Dispose() {
            Dispose(true);
        }

        #endregion
    }
    #endregion
}
