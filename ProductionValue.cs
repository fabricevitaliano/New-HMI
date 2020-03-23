using System;
using System.IO;
using Sidel.Core.Framework.Args;
using Sidel.Core.Framework.Logging;
using Sidel.Core.Production.Information.DAO;
using zenOn;

namespace Sidel.Core.Production.Information.Model
{
    /// <summary>
    ///     Class that stores information about a production value.
    /// </summary>
    public class ProductionValue : IDisposable
    {
        #region ctors

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="translationProvider">Object that translates the label</param>
        /// <param name="zenonVariableRetriever">Object that retrieves the Zenon variable reference</param>
        /// <param name="projectName">Name of the project containing the searched variable</param>
        /// <param name="variableName">Name of the searched variable</param>
        /// <param name="labelTranslationKey">Translation key of the label</param>
        public ProductionValue(ITranslationProvider translationProvider, ZenonVariableRetriever zenonVariableRetriever, string projectName, string variableName, string labelTranslationKey)
        {
            // Cache dependencies
            _labelTranslationKey = labelTranslationKey;
            ProjectName = projectName;
            VariableName = variableName;
            _translationProvider = translationProvider;
            _variableRetriever = zenonVariableRetriever;

            // Initialize logger
            _logger = new Logger(new DirectoryInfo(@"D:\DATA\ERRORSLOGS\ProductionInformation"));

            Initialize();

            // Manage events
            _variableRetriever.VariableChanged += _variableRetriever_VariableChanged;
            _translationProvider.LanguageChanged += TranslationProviderLanguageChanged;
        }

        

        #endregion

        #region fields and properties

        /// <summary>
        /// Translation key of the label
        /// </summary>
        private readonly string _labelTranslationKey;

        /// <summary>
        /// Object that logs messages in log files.
        /// </summary>
        private readonly Logger _logger;

        /// <summary>
        /// Object that translates terms.
        /// </summary>
        private readonly ITranslationProvider _translationProvider;

        /// <summary>
        /// Object that retrieves a Zenon variable.
        /// </summary>
        private readonly ZenonVariableRetriever _variableRetriever;

        /// <summary>
        /// The last known unit of the variable
        /// </summary>
        private string _cachedUnit;

        /// <summary>
        /// Last known value of the variable.
        /// </summary>
        private object _cachedValue;

        /// <summary>
        /// Name of the project containing the searched variable.
        /// </summary>
        public string ProjectName { get; }

        /// <summary>
        /// Name of the searched variable.
        /// </summary>
        public string VariableName { get; }

        /// <summary>
        /// Last known value of the variable.
        /// </summary>
        public object CachedValue
        {
            get
            {
                if (_cachedValue == null) Initialize();
                return _cachedValue;
            }
            set
            {
                _cachedValue = value;
                OnValueChanged();
            }
        }

        /// <summary>
        /// String format for displaying the variable's value.
        /// </summary>
        public string StringFormat { get; set; }

        /// <summary>
        /// Display format in the interface.
        /// </summary>
        public DisplayFormats DisplayFormat { get; set; }

        /// <summary>
        /// Last known unit of the variable
        /// </summary>
        public string Unit
        {
            get { return _cachedUnit; }
            private set
            {
                if (String.Equals(_cachedUnit,
                                  value)) return;
                _cachedUnit = value;
                OnUnitChanged();
            }
        }

        /// <summary>
        /// Label of the production value
        /// </summary>
        public string Label
        {
            get
            {
                if (_translationProvider == null) return _labelTranslationKey;
                return _translationProvider.Translate(_labelTranslationKey);
            }
        }

        #endregion

        /// <summary>
        /// Dispose of the production value
        /// </summary>
        public void Dispose()
        {
            _variableRetriever?.Dispose();
            _logger?.Dispose();
        }

        public override string ToString()
        {
            return $"ProductionValue. Label: {Label}\tProjectName: {ProjectName}\tVariableName: {VariableName}\tCachedValue: {CachedValue}\tStringFormat: {StringFormat}\tDisplayFormat: {DisplayFormat}\tUnit: {Unit}";
        }

        /// <summary>
        /// Initialize the production value
        /// </summary>
        private void Initialize()
        {
#if DEBUG
            _logger.Debug($"Initializing production value...");
#endif
            if (_cachedValue == null)
            {
                // Try to retrieve the variable
                if (!_variableRetriever.TryRetrieveVariable(ProjectName,
                                                            VariableName,
                                                            out _))
                {
                    _logger.Warn($"Could not retrieve variable {ProjectName}#{VariableName}");
                }
            }
        }

        /// <summary>
        /// Manage a change in the language
        /// </summary>
        /// <param name="sender">Unused</param>
        /// <param name="e">Unused</param>
        private void TranslationProviderLanguageChanged(object sender, EventArgs e)
        {
            OnLabelChanged();
        }

        private void _variableRetriever_VariableChanged(object sender, ZenonVariableEventArgs e)
        {
            if (!e.VariableName.Equals(VariableName)) return;
#if DEBUG
            _logger.Debug($"New value found for variable {e.VariableName}");
#endif
            CachedValue = e.Value;
            Unit = e.Unit;
        }

        /// <summary>
        /// Manage a change in the variable's value
        /// </summary>
        /// <param name="sender">Unused</param>
        /// <param name="e">Information about the value change</param>
        private void _variableRetriever_ValueChanged(object sender, ValueEventArgs e)
        {
            IVariable variable = e.Value as IVariable;
            if (variable == null || !variable.Name.Equals(VariableName)) return;

            CachedValue = variable.Value[0];
            Unit = variable.Unit;
        }

        /// <summary>
        /// Event triggered when the value changes
        /// </summary>
        public event EventHandler<EventArgs> ValueChanged;

        private void OnValueChanged()
        {
            if (ValueChanged != null)
            {
                ValueChanged(this,
                             EventArgs.Empty);
            }
        }


        /// <summary>
        /// Event triggered when the label changes
        /// </summary>
        public event EventHandler<EventArgs> LabelChanged;

        private void OnLabelChanged()
        {
            if (LabelChanged != null)
            {
                LabelChanged(this,
                             EventArgs.Empty);
            }
        }

        /// <summary>
        /// Event triggered when a new unit is active on the variable
        /// </summary>
        public event EventHandler<EventArgs> UnitChanged;

        private void OnUnitChanged()
        {
            if (UnitChanged != null)
            {
                UnitChanged(this,
                            EventArgs.Empty);
            }
        }
    }
}