using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Collections;
using System.Xml.Linq;
using System.Globalization;

using DynamicLinqPadPostgreSqlDriver.Shared.Extensions;
using DynamicLinqPadPostgreSqlDriver.Shared.Helpers;
using DynamicLinqPadPostgreSqlDriver.UI.Helpers;
using LINQPad.Extensibility.DataContext;
using DynamicLinqPadPostgreSqlDriver.Shared;

namespace DynamicLinqPadPostgreSqlDriver.UI.ViewModels
{
   class ConnectionInfoViewModel : INotifyPropertyChanged, INotifyDataErrorInfo
   {
      private readonly IConnectionInfo _cxInfo;

      private readonly Func<string> _passwordGetter;

      #region Properties

      private string _name;

      public string Name
      {
         get { return _name; }
         set
         {
            if (Equals(_name, value))
               return;

            _name = value;
            OnPropertyChanged(nameof(Name));
         }
      }

      private string _server;

      public string Server
      {
         get { return _server; }
         set
         {
            if (Equals(_server, value))
               return;

            _server = value;
            OnPropertyChanged(nameof(Server));
         }
      }

      private string _database;

      public string Database
      {
         get { return _database; }
         set
         {
            if (Equals(_database, value))
               return;

            _database = value;
            OnPropertyChanged(nameof(Database));
         }
      }

      private string _userName;

      public string UserName
      {
         get { return _userName; }
         set
         {
            if (Equals(_userName, value))
               return;

            _userName = value;
            OnPropertyChanged(nameof(UserName));
         }
      }

      private string _connectionString;

      public string ConnectionString
      {
         get { return _connectionString; }
         set
         {
            if (Equals(_connectionString, value))
               return;

            _connectionString = value;
            OnPropertyChanged(nameof(ConnectionString));
         }
      }

      private bool _useConnectionInfo;

      public bool UseConnectionInfo
      {
         get { return _useConnectionInfo; }
         set
         {
            if (Equals(_useConnectionInfo, value))
               return;

            _useConnectionInfo = value;
            OnPropertyChanged(nameof(UseConnectionInfo));
         }
      }

      private bool _useConnectionString;

      public bool UseConnectionString
      {
         get { return _useConnectionString; }
         set
         {
            if (Equals(_useConnectionString, value))
               return;

            _useConnectionString = value;
            OnPropertyChanged(nameof(UseConnectionString));
         }
      }

      private bool _pluralizeSetAndTableProperties;

      public bool PluralizeSetAndTableProperties
      {
         get { return _pluralizeSetAndTableProperties; }
         set
         {
            if (Equals(_pluralizeSetAndTableProperties, value))
               return;

            _pluralizeSetAndTableProperties = value;
            OnPropertyChanged(nameof(PluralizeSetAndTableProperties));
         }
      }

      private bool _singularizeEntityNames;

      public bool SingularizeEntityNames
      {
         get { return _singularizeEntityNames; }
         set
         {
            if (Equals(_singularizeEntityNames, value))
               return;

            _singularizeEntityNames = value;
            OnPropertyChanged(nameof(SingularizeEntityNames));
         }
      }

      private bool _capitalizePropertiesTablesAndColumns;

      public bool CapitalizePropertiesTablesAndColumns
      {
         get { return _capitalizePropertiesTablesAndColumns; }
         set
         {
            if (Equals(_capitalizePropertiesTablesAndColumns, value))
               return;

            _capitalizePropertiesTablesAndColumns = value;
            OnPropertyChanged(nameof(CapitalizePropertiesTablesAndColumns));
         }
      }

      private bool _useAdvancedDataTypes;

      public bool UseAdvancedDataTypes
      {
         get { return _useAdvancedDataTypes; }
         set
         {
            if (Equals(_useAdvancedDataTypes, value))
               return;

            _useAdvancedDataTypes = value;
            OnPropertyChanged(nameof(UseAdvancedDataTypes));
         }
      }

      private bool _canSave;

      public bool CanSave
      {
         get { return _canSave; }
         set
         {
            if (Equals(_canSave, value))
               return;

            _canSave = value;
            OnPropertyChanged(nameof(CanSave));
         }
      }

      private bool? _dialogResult;

      public bool? DialogResult
      {
         get { return _dialogResult; }
         set
         {
            if (Equals(_dialogResult, value))
               return;

            _dialogResult = value;
            OnPropertyChanged(nameof(DialogResult));
         }
      }

      private bool _hasErrors;

      public bool HasErrors
      {
         get { return _hasErrors; }
         set
         {
            if (Equals(_hasErrors, value))
               return;

            _hasErrors = value;
            OnPropertyChanged(nameof(HasErrors));
         }
      }

      public ICommand SaveCommand { get;  }

      public ICommand TestConnectionCommand { get; }

      #endregion

      public ConnectionInfoViewModel()
      {
         // empty constructor for designer support
      }

      public ConnectionInfoViewModel(IConnectionInfo cxInfo, Func<string> passwordGetter, Action<string> passwordSetter)
      {
         _cxInfo = cxInfo;

         _passwordGetter = passwordGetter;

         _name = cxInfo.DisplayName;

         _server = cxInfo.DatabaseInfo.Server;
         _database = cxInfo.DatabaseInfo.Database;
         _userName = cxInfo.DatabaseInfo.UserName;

         if (!string.IsNullOrEmpty(cxInfo.DatabaseInfo.Password))
         {
            passwordSetter(cxInfo.DatabaseInfo.Password);
         }

         if (string.IsNullOrWhiteSpace(cxInfo.DatabaseInfo.CustomCxString))
         {
            _useConnectionInfo = true;
         }
         else
         {
            _useConnectionString = true;
            _connectionString = cxInfo.DatabaseInfo.CustomCxString;
         }

         SaveCommand = new DelegatingCommand(() => Save());
         // ToDo | http://www.amazedsaint.com/2010/10/asynchronous-delegate-command-for-your.html ?
         TestConnectionCommand = new DelegatingCommand(async () => await TestConnection());

         LoadDriverData(cxInfo.DriverData);

         PropertyChanged += (sender, args) =>
         {
            switch (args.PropertyName)
            {
               case "Server":
               case "Database":
               case "ConnectionString":
               case "UseConnectionInfo":
               case "UseConnectionString":
                  UpdateCanSave();
                  UpdateHasErrors();
                  break;
            }
         };

         UpdateCanSave();
         UpdateHasErrors();
      }

      private void LoadDriverData(XElement driverData)
      {
         _pluralizeSetAndTableProperties = driverData.GetDescendantValue(DriverOption.PluralizeSetAndTableProperties, Convert.ToBoolean, true);
         _singularizeEntityNames = driverData.GetDescendantValue(DriverOption.SingularizeEntityNames, Convert.ToBoolean, true);
         _capitalizePropertiesTablesAndColumns = driverData.GetDescendantValue(DriverOption.CapitalizePropertiesTablesAndColumns, Convert.ToBoolean, true);
         _useAdvancedDataTypes = driverData.GetDescendantValue(DriverOption.UseExperimentalTypes, Convert.ToBoolean, false);
      }

      private void SetDriverData(XElement driverData)
      {
         driverData.RemoveAll();

         var xElement = new XElement(DriverOption.PluralizeSetAndTableProperties.ToString());
         xElement.Value = PluralizeSetAndTableProperties.ToString(CultureInfo.InvariantCulture);
         driverData.Add(xElement);

         xElement = new XElement(DriverOption.SingularizeEntityNames.ToString());
         xElement.Value = SingularizeEntityNames.ToString(CultureInfo.InvariantCulture);
         driverData.Add(xElement);

         xElement = new XElement(DriverOption.CapitalizePropertiesTablesAndColumns.ToString());
         xElement.Value = CapitalizePropertiesTablesAndColumns.ToString(CultureInfo.InvariantCulture);
         driverData.Add(xElement);

         xElement = new XElement(DriverOption.UseExperimentalTypes.ToString());
         xElement.Value = UseAdvancedDataTypes.ToString(CultureInfo.InvariantCulture);
         driverData.Add(xElement);
      }

      private void Save()
      {
         if (_cxInfo == null)
            return;

         _cxInfo.DisplayName = Name;

         if (UseConnectionInfo)
         {
            _cxInfo.DatabaseInfo.Server = Server;
            _cxInfo.DatabaseInfo.UserName = UserName;
            _cxInfo.DatabaseInfo.Password = _passwordGetter();
            _cxInfo.DatabaseInfo.Database = Database;

            _cxInfo.DatabaseInfo.CustomCxString = null;
            _cxInfo.DatabaseInfo.EncryptCustomCxString = false;
         }
         else
         {
            _cxInfo.DatabaseInfo.Server = null;
            _cxInfo.DatabaseInfo.UserName = null;
            _cxInfo.DatabaseInfo.Password = null;
            _cxInfo.DatabaseInfo.Database = null;

            _cxInfo.DatabaseInfo.CustomCxString = ConnectionString;
            _cxInfo.DatabaseInfo.EncryptCustomCxString = true;
         }

         SetDriverData(_cxInfo.DriverData);

         DialogResult = true;
      }

      private async Task TestConnection()
      {
         try
         {
            var success = UseConnectionInfo
               ? await ConnectionHelper.CheckConnection(Server, Database, UserName, _passwordGetter())
               : await ConnectionHelper.CheckConnection(ConnectionString);

            if (!success)
            {
               MessageBox.Show("Unable to connect to the server.", "PostgreSQL Connection", MessageBoxButton.OK, MessageBoxImage.Error);
               return;
            }

            MessageBox.Show("Connection successful.", "PostgreSQL Connection", MessageBoxButton.OK, MessageBoxImage.Information);
         }
         catch (Exception ex)
         {
            // http://stackoverflow.com/questions/26220254/database-connection-error-associated-with-the-encoding
            MessageBox.Show($"Unable to connect to the server:\n\n{ex.Message}", "PostgreSQL Connection", MessageBoxButton.OK, MessageBoxImage.Error);
         }
      }

      private void UpdateCanSave()
      {
         CanSave = (UseConnectionInfo && !string.IsNullOrWhiteSpace(Server) && !string.IsNullOrWhiteSpace(Database))
                   || (UseConnectionString && !string.IsNullOrWhiteSpace(ConnectionString));
      }

      private void UpdateHasErrors()
      {
         HasErrors = (UseConnectionInfo && string.IsNullOrWhiteSpace(Server) || string.IsNullOrWhiteSpace(Database))
                   || (UseConnectionString && string.IsNullOrWhiteSpace(ConnectionString));

         OnErrorsChanged(nameof(Server));
         OnErrorsChanged(nameof(Database));
         OnErrorsChanged(nameof(ConnectionString));
      }

      public event PropertyChangedEventHandler PropertyChanged;

      private void OnPropertyChanged(string propertyName)
      {
         var handler = PropertyChanged;
         handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }

      public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

      private void OnErrorsChanged(string propertyName)
      {
         var handler = ErrorsChanged;
         handler?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
      }

      public IEnumerable GetErrors(string propertyName)
      {
         switch (propertyName)
         {
            case nameof(Server):
               if (UseConnectionInfo && string.IsNullOrWhiteSpace(Server))
                  yield return "The server field is required.";
               break;
            case nameof(Database):
               if (UseConnectionInfo && string.IsNullOrWhiteSpace(Database))
                  yield return "The database field is required.";
               break;
            case nameof(ConnectionString):
               if (UseConnectionString && string.IsNullOrWhiteSpace(ConnectionString))
                  yield return "The connection string field is required.";
               break;
         }
      }
   }
}
