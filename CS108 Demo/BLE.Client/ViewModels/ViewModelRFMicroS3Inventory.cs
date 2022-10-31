using Acr.UserDialogs;
using System;
using System.IO;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;
using MvvmCross.Core.ViewModels;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions;
using Prism.Mvvm;
using Xamarin.Forms;

// NEEDED FOR CSV EXPORT
using System.Data.SqlTypes;
using System.Text;

// using LiveChartsCore;
// using LiveChartsCore.Defaults;
// using LiveChartsCore.SkiaSharpView;
// using LiveChartsCore.SkiaSharpView.Painting;
// using LiveChartsCore.Drawing;
// using LiveChartsCore.Kernel;
// using LiveChartsCore.Kernel.Drawing;
// using LiveChartsCore.Kernel.Sketches;
// using LiveChartsCore.Measure;
// using SkiaSharp;


namespace BLE.Client.ViewModels {

    public class CsvExport {
		List<string> _fields = new List<string>();
		List<Dictionary<string, object>> _rows = new List<Dictionary<string, object>>();
		Dictionary<string, object> _currentRow {get {return _rows[_rows.Count - 1];}}
		private readonly string _columnSeparator;
		private readonly bool _includeColumnSeparatorDefinitionPreamble;
		private readonly bool _includeHeaderRow;

		/// <summary>
		/// Initializes a new instance of the <see cref="Jitbit.Utils.CsvExport"/> class.
		/// </summary>
		/// <param name="columnSeparator">
		/// The string used to separate columns in the output.
		/// By default this is a comma so that the generated output is a CSV file.
		/// </param>
		/// <param name="includeColumnSeparatorDefinitionPreamble">
		/// Whether to include the preamble that declares which column separator is used in the output.
		/// By default this is <c>true</c> so that Excel can open the generated CSV
		/// without asking the user to specify the delimiter used in the file.
		/// </param>
		/// <param name="includeHeaderRow">
		/// Whether to include the header row with the columns names in the export
		/// </param>
		public CsvExport(string columnSeparator = ",", bool includeColumnSeparatorDefinitionPreamble = true, bool includeHeaderRow = true) {
			_columnSeparator = columnSeparator;
			_includeColumnSeparatorDefinitionPreamble = includeColumnSeparatorDefinitionPreamble;
			_includeHeaderRow = includeHeaderRow;
		}

		public object this[string field] {
			set {
				// Keep track of the field names, because the dictionary loses the ordering
				if (!_fields.Contains(field)) _fields.Add(field);
				_currentRow[field] = value;
			}
		}

		public void AddRow() {
			_rows.Add(new Dictionary<string, object>());
		}

		public void AddRows<T>(IEnumerable<T> list) {
			if (list.Any()) {
				foreach (var obj in list) {
					AddRow();
					var values = obj.GetType().GetProperties();
					foreach (var value in values) {
						this[value.Name] = value.GetValue(obj, null);
					}
				}
			}
		}

		/// <summary>
		/// Converts a value to how it should output in a csv file
		/// If it has a comma, it needs surrounding with double quotes
		/// Eg Sydney, Australia -> "Sydney, Australia"
		/// Also if it contains any double quotes ("), then they need to be replaced with quad quotes[sic] ("")
		/// Eg "Dangerous Dan" McGrew -> """Dangerous Dan"" McGrew"
		/// </summary>
		/// <param name="columnSeparator">
		/// The string used to separate columns in the output.
		/// By default this is a comma so that the generated output is a CSV document.
		/// </param>
		public static string MakeValueCsvFriendly(object value, string columnSeparator=",") {
			if (value == null) return "";
			if (value is INullable && ((INullable)value).IsNull) return "";

			string output;
			if (value is DateTime) {
				if (((DateTime)value).TimeOfDay.TotalSeconds == 0) {
					output = ((DateTime)value).ToString("yyyy-MM-dd");
				}
				else {
					output = ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss");
				}
			}
			else {
				output = value.ToString().Trim();
			}

			if (output.Length > 30000) //cropping value for stupid Excel
				output = output.Substring(0, 30000);

			if (output.Contains(columnSeparator) || output.Contains("\"") || output.Contains("\n") || output.Contains("\r"))
				output = '"' + output.Replace("\"", "\"\"") + '"';
			
			return output;
		}

		private IEnumerable<string> ExportToLines() {
			if (_includeColumnSeparatorDefinitionPreamble) yield return "sep=" + _columnSeparator;

			// The header
			if (_includeHeaderRow)
				yield return string.Join(_columnSeparator, _fields.Select(f => MakeValueCsvFriendly(f, _columnSeparator)));

			// The rows
			foreach (Dictionary<string, object> row in _rows) {
				foreach (string k in _fields.Where(f => !row.ContainsKey(f))) {
					row[k] = null;
				}
				yield return string.Join(_columnSeparator, _fields.Select(field => MakeValueCsvFriendly(row[field], _columnSeparator)));
			}
		}

		public string Export() {
			StringBuilder sb = new StringBuilder();
			foreach (string line in ExportToLines()) {
				sb.AppendLine(line);
			}
			return sb.ToString();
		}

		public void ExportToFile(string path) {
			File.WriteAllBytes(path, ExportToBytes());
		}

		public byte[] ExportToBytes() {
			var data = Encoding.Unicode.GetBytes(Export());
			return Encoding.Unicode.GetPreamble().Concat(data).ToArray();
		}
	}

    public class ViewModelRFMicroS3Inventory : BaseViewModel {

        public class RFMicroTagInfoViewModel : BindableBase {

            /////////////////////////////////////////////////////////////////////////////////////////////////////////
            // CLASS UPDATES/ADDITIONS
            private string _TimeString; // Time at which last tag was read
            public string TimeString {get {return this._TimeString;} set{this.SetProperty(ref this._TimeString, value);}}
            private DateTime _CurrentTime; // DateTime object for Live Plotting Comparison
            public DateTime CurrentTime {get {return this._CurrentTime;} set{this.SetProperty(ref this._CurrentTime, value);}}
            private double _SAV;
            public double SAV;
            /////////////////////////////////////////////////////////////////////////////////////////////////////////

            private string _EPC;
            public string EPC {get {return this._EPC;} set {this.SetProperty(ref this._EPC, value);}}
            private string _NickName;
            public string NickName {get {return this._NickName;} set {this.SetProperty(ref this._NickName, value);}}
            private string _DisplayName;
            public string DisplayName {get {return this._DisplayName;} set {this.SetProperty(ref this._DisplayName, value);}}
            private uint _OCRSSI;
            public uint OCRSSI {get {return this._OCRSSI;} set {this.SetProperty(ref this._OCRSSI, value);}}
            public double _sensorValueSum;
            private string _sensorAvgValue;
            public string SensorAvgValue {get {return this._sensorAvgValue;} set {this.SetProperty(ref this._sensorAvgValue, value);}}
            private uint _sucessCount;
            public uint SucessCount {get {return this._sucessCount;} set {this.SetProperty(ref this._sucessCount, value);}}
            private string _RSSIColor;
            public string RSSIColor {get {return this._RSSIColor;} set {this.SetProperty(ref this._RSSIColor, value);}}
            private string _Performance;
            public string Performance {get {return this._Performance;} set {this.SetProperty(ref this._Performance, value);}}

            public RFMicroTagInfoViewModel() {}
        }

        private readonly IUserDialogs _userDialogs;

        #region -------------- RFID inventory -----------------

        public ICommand OnStartInventoryButtonCommand {protected set; get;}
        public ICommand OnClearButtonCommand {protected set; get;}
        public ICommand OnShareDataCommand {protected set; get;}
        public ICommand OnTestCommand {protected set; get;}

        private ObservableCollection<RFMicroTagInfoViewModel> _TagInfoList = new ObservableCollection<RFMicroTagInfoViewModel>();
        public ObservableCollection<RFMicroTagInfoViewModel> TagInfoList {get {return _TagInfoList;} set {SetProperty(ref _TagInfoList, value);}}

        private string _startInventoryButtonText = "Start Inventory";
        public string startInventoryButtonText {get {return _startInventoryButtonText;}}

        bool _tagCount = false;

        /////////////////////////////////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////////
        // For Saving Data / CSV exporting
        List<string> tag_List = new List<string>();
        Dictionary<string, List<string>> tag_Time = new Dictionary<string, List<string>>();
        Dictionary<string, List<string>> tag_Data = new Dictionary<string, List<string>>();
        Dictionary<string, List<string>> tag_RSSI = new Dictionary<string, List<string>>();

        // Private List to hold Graphed Data
        // private List<LineSeries<double>> _graph = new List<LineSeries<double>>{
        //     new LiveChartsCore.SkiaSharpView.LineSeries<double> {
        //         Name = "Left Quad",
        //         Values = new double[]{25.0},
        //         Fill = null
        //     },
        //     new LiveChartsCore.SkiaSharpView.LineSeries<double> {
        //         Name = "Right Quad",
        //         Values = new double[]{25.0},
        //         Fill = null
        //     },
        //     new LiveChartsCore.SkiaSharpView.LineSeries<double> {
        //         Name = "Left Glute",
        //         Values = new double[]{25.0},
        //         Fill = null
        //     },
        //     new LiveChartsCore.SkiaSharpView.LineSeries<double> {
        //         Name = "Right Glute",
        //         Values = new double[]{25.0},
        //         Fill = null
        //     },
        //     new LiveChartsCore.SkiaSharpView.LineSeries<double> {
        //         Name = "Left Calf",
        //         Values = new double[]{25.0},
        //         Fill = null
        //     },
        //     new LiveChartsCore.SkiaSharpView.LineSeries<double> {
        //         Name = "Right Calf",
        //         Values = new double[]{25.0},
        //         Fill = null
        //     }
        // };

        // Public Object for Active Updating
        // public List<LineSeries<double>> graph {
        //     get {return this._graph;}
        //     set {
        //         this._graph = value;
        //         OnPropertyChanged("graph");
        //     }
        // }

        ///////////////// Public/Private Variables for Body Model /////////////////
        private string _LeftGluteTemp; public string LeftGluteTemp {get => _LeftGluteTemp; set {_LeftGluteTemp = value; OnPropertyChanged("LeftGluteTemp");}}
        private string _RightGluteTemp; public string RightGluteTemp {get => _RightGluteTemp; set {_RightGluteTemp = value; OnPropertyChanged("RightGluteTemp");}}
        private string _LeftQuadTemp; public string LeftQuadTemp {get => _LeftQuadTemp; set {_LeftQuadTemp = value; OnPropertyChanged("LeftQuadTemp");}}
        private string _RightQuadTemp; public string RightQuadTemp {get => _RightQuadTemp; set {_RightQuadTemp = value; OnPropertyChanged("RightQuadTemp");}}
        private string _LeftCalfTemp; public string LeftCalfTemp {get => _LeftCalfTemp; set {_LeftCalfTemp = value; OnPropertyChanged("LeftCalfTemp");}}
        private string _RightCalfTemp; public string RightCalfTemp {get => _RightCalfTemp; set {_RightCalfTemp = value; OnPropertyChanged("RightCalfTemp");}}
        private string _LeftGlute; public string LeftGlute {get => _LeftGlute; set {_LeftGlute = value; OnPropertyChanged("LeftGlute");}}
        private string _RightGlute; public string RightGlute {get => _RightGlute; set {_RightGlute = value; OnPropertyChanged("RightGlute");}}
        private string _LeftQuad; public string LeftQuad {get => _LeftQuad; set {_LeftQuad = value; OnPropertyChanged("LeftQuad");}}
        private string _RightQuad; public string RightQuad {get => _RightQuad; set {_RightQuad = value; OnPropertyChanged("RightQuad");}}
        private string _LeftCalf; public string LeftCalf {get => _LeftCalf; set {_LeftCalf = value; OnPropertyChanged("LeftCalf");}}
        private string _RightCalf; public string RightCalf {get => _RightCalf; set {_RightCalf = value; OnPropertyChanged("RightCalf");}}

        private List<string> _epcs; public List<string> epcs {get => _epcs; set {_epcs = value; OnPropertyChanged("epcs");}}
        private Dictionary<string, string> _map; public Dictionary<string, string> map {get => _map; set {_map = value; OnPropertyChanged("map");}}

        ///////////////////////////////////////////////////////////////////////////

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////////

        private string _labelVoltage = "";
        public string labelVoltage {get {return _labelVoltage;}}
        public bool _startInventory = true;
        bool _cancelVoltageValue = false;

        #endregion

        public ViewModelRFMicroS3Inventory(IAdapter adapter, IUserDialogs userDialogs) : base(adapter) {
            _userDialogs = userDialogs;

            LeftGluteTemp  = "N/A";
            RightGluteTemp = "N/A";  // Default Label values
            LeftQuadTemp   = "N/A";
            RightQuadTemp  = "N/A";
            LeftCalfTemp   = "N/A";
            RightCalfTemp  = "N/A";

            LeftGlute   = "gray";
            RightGlute  = "gray";  // Default Image colors
            LeftQuad    = "gray";
            RightQuad   = "gray";
            LeftCalf    = "gray";
            RightCalf   = "gray";

            map = new Dictionary<string, string> {
                {"E282403E000207D6F9773098", "Left Glute"},
                {"E282403E000207D6F97748C0", "Right Glute"},
                {"E282403E000207D6F9770273", "Left Quad"},
                {"E282403E000207D6F97786D2", "Right Quad"},
                {"E282403E000207D6F9779863", "Left Calf"},
                {"E282403E000207D6F9775A25", "Right Calf"},
            };

            epcs = new List<string> {
                "E282403E000207D6F9770273",  // Left Quad
                "E282403E000207D6F97786D2",  // Right Quad
                "E282403E000207D6F9773098",  // Left Glute
                "E282403E000207D6F97748C0",  // Right Glute
                "E282403E000207D6F9779863",  // Left Calf
                "E282403E000207D6F9775A25",  // Right Calf
            };

            OnStartInventoryButtonCommand = new Command(StartInventoryClick);
            OnClearButtonCommand = new Command(ClearClick);
            OnShareDataCommand = new Command(ShareDataButtonClick);
        }

        ~ViewModelRFMicroS3Inventory() {}

        public override void Resume() {
            base.Resume();

            // RFID event handler
            BleMvxApplication._reader1.rfid.OnAsyncCallback += new EventHandler<CSLibrary.Events.OnAsyncCallbackEventArgs>(TagInventoryEvent);
            BleMvxApplication._reader2.rfid.OnAsyncCallback += new EventHandler<CSLibrary.Events.OnAsyncCallbackEventArgs>(TagInventoryEvent);

            // Key Button event handler
            BleMvxApplication._reader1.notification.OnKeyEvent += new EventHandler<CSLibrary.Notification.HotKeyEventArgs>(HotKeys_OnKeyEvent);
            BleMvxApplication._reader1.notification.OnVoltageEvent += new EventHandler<CSLibrary.Notification.VoltageEventArgs>(VoltageEvent1);

            BleMvxApplication._reader2.notification.OnKeyEvent += new EventHandler<CSLibrary.Notification.HotKeyEventArgs>(HotKeys_OnKeyEvent);
            BleMvxApplication._reader2.notification.OnVoltageEvent += new EventHandler<CSLibrary.Notification.VoltageEventArgs>(VoltageEvent2);

            InventorySetting();
        }

        public override void Suspend() {
            BleMvxApplication._reader1.rfid.CancelAllSelectCriteria();                // Confirm cancel all filter
            BleMvxApplication._reader1.rfid.StopOperation();
            BleMvxApplication._reader2.rfid.CancelAllSelectCriteria();                // Confirm cancel all filter
            BleMvxApplication._reader2.rfid.StopOperation();
            ClassBattery.SetBatteryMode(ClassBattery.BATTERYMODE.IDLE);
            BleMvxApplication._reader1.barcode.Stop();
            BleMvxApplication._reader2.barcode.Stop();

            // Cancel RFID event handler
            BleMvxApplication._reader1.rfid.OnAsyncCallback -= new EventHandler<CSLibrary.Events.OnAsyncCallbackEventArgs>(TagInventoryEvent);
            BleMvxApplication._reader1.rfid.OnStateChanged += new EventHandler<CSLibrary.Events.OnStateChangedEventArgs>(StateChangedEvent1);

            BleMvxApplication._reader2.rfid.OnAsyncCallback -= new EventHandler<CSLibrary.Events.OnAsyncCallbackEventArgs>(TagInventoryEvent);
            BleMvxApplication._reader2.rfid.OnStateChanged += new EventHandler<CSLibrary.Events.OnStateChangedEventArgs>(StateChangedEvent2);

            // Key Button event handler
            BleMvxApplication._reader1.notification.OnKeyEvent -= new EventHandler<CSLibrary.Notification.HotKeyEventArgs>(HotKeys_OnKeyEvent);
            BleMvxApplication._reader1.notification.OnVoltageEvent -= new EventHandler<CSLibrary.Notification.VoltageEventArgs>(VoltageEvent1);

            BleMvxApplication._reader2.notification.OnKeyEvent -= new EventHandler<CSLibrary.Notification.HotKeyEventArgs>(HotKeys_OnKeyEvent);
            BleMvxApplication._reader2.notification.OnVoltageEvent -= new EventHandler<CSLibrary.Notification.VoltageEventArgs>(VoltageEvent2);

            base.Suspend();
        }

        protected override void InitFromBundle(IMvxBundle parameters) {
            base.InitFromBundle(parameters);
        }

        private void ClearClick() {
            InvokeOnMainThread(() => {
                lock (TagInfoList) {
                    TagInfoList.Clear();
                }
            });
        }

        public RFMicroTagInfoViewModel objItemSelected {
            set {
                if (value != null) {
                    BleMvxApplication._SELECT_EPC = value.EPC;
                    ShowViewModel<ViewModelRFMicroReadTemp>(new MvxBundle());
                }
            }
            get => null;
        }

        void InventorySetting() {
            switch (BleMvxApplication._config1.RFID_FrequenceSwitch) { // READER #1
                case 0:
                    BleMvxApplication._reader1.rfid.SetHoppingChannels(BleMvxApplication._config1.RFID_Region);
                    break;
                case 1:
                    BleMvxApplication._reader1.rfid.SetFixedChannel(BleMvxApplication._config1.RFID_Region, BleMvxApplication._config1.RFID_FixedChannel);
                    break;
                case 2:
                    BleMvxApplication._reader1.rfid.SetAgileChannels(BleMvxApplication._config1.RFID_Region);
                    break;
            }
            switch (BleMvxApplication._config2.RFID_FrequenceSwitch) { // READER #2
                case 0:
                    BleMvxApplication._reader2.rfid.SetHoppingChannels(BleMvxApplication._config2.RFID_Region);
                    break;
                case 1:
                    BleMvxApplication._reader2.rfid.SetFixedChannel(BleMvxApplication._config2.RFID_Region, BleMvxApplication._config2.RFID_FixedChannel);
                    break;
                case 2:
                    BleMvxApplication._reader2.rfid.SetAgileChannels(BleMvxApplication._config2.RFID_Region);
                    break;
            }
            
            BleMvxApplication._reader1.rfid.Options.TagRanging.flags = CSLibrary.Constants.SelectFlags.ZERO;
            BleMvxApplication._reader2.rfid.Options.TagRanging.flags = CSLibrary.Constants.SelectFlags.ZERO;

            // Setting 1
            SetPower(BleMvxApplication._rfMicro_Power);

            // Setting 3  // MUST SET for RFMicro
            BleMvxApplication._config1.RFID_DynamicQParms.toggleTarget = (BleMvxApplication._rfMicro_Target == 2) ? 1U : 0U;
            BleMvxApplication._config2.RFID_DynamicQParms.toggleTarget = (BleMvxApplication._rfMicro_Target == 2) ? 1U : 0U;
            BleMvxApplication._config1.RFID_DynamicQParms.retryCount = 5; // for RFMicro special setting
            BleMvxApplication._config2.RFID_DynamicQParms.retryCount = 5; // for RFMicro special setting

            BleMvxApplication._reader1.rfid.SetDynamicQParms(BleMvxApplication._config1.RFID_DynamicQParms);
            BleMvxApplication._config1.RFID_DynamicQParms.retryCount = 0; // reset to normal

            BleMvxApplication._reader2.rfid.SetDynamicQParms(BleMvxApplication._config2.RFID_DynamicQParms);
            BleMvxApplication._config2.RFID_DynamicQParms.retryCount = 0; // reset to normal

            // Setting 4
            BleMvxApplication._config1.RFID_FixedQParms.toggleTarget = (BleMvxApplication._rfMicro_Target == 2) ? 1U : 0U;
            BleMvxApplication._config1.RFID_FixedQParms.retryCount = 5; // for RFMicro special setting
            BleMvxApplication._reader1.rfid.SetFixedQParms(BleMvxApplication._config1.RFID_FixedQParms);
            BleMvxApplication._config1.RFID_FixedQParms.retryCount = 0; // reset to normal

            BleMvxApplication._config2.RFID_FixedQParms.toggleTarget = (BleMvxApplication._rfMicro_Target == 2) ? 1U : 0U;
            BleMvxApplication._config2.RFID_FixedQParms.retryCount = 5; // for RFMicro special setting
            BleMvxApplication._reader2.rfid.SetFixedQParms(BleMvxApplication._config2.RFID_FixedQParms);
            BleMvxApplication._config2.RFID_FixedQParms.retryCount = 0; // reset to normal

            // Setting 2
            BleMvxApplication._reader1.rfid.SetOperationMode(BleMvxApplication._config1.RFID_OperationMode);
            BleMvxApplication._reader1.rfid.SetTagGroup(CSLibrary.Constants.Selected.ASSERTED, BleMvxApplication._config1.RFID_TagGroup.session, (BleMvxApplication._rfMicro_Target != 1) ? CSLibrary.Constants.SessionTarget.A : CSLibrary.Constants.SessionTarget.B);
            BleMvxApplication._reader1.rfid.SetCurrentSingulationAlgorithm(BleMvxApplication._config1.RFID_Algorithm);
            BleMvxApplication._reader1.rfid.SetCurrentLinkProfile(BleMvxApplication._config1.RFID_Profile);

            BleMvxApplication._reader2.rfid.SetOperationMode(BleMvxApplication._config2.RFID_OperationMode);
            BleMvxApplication._reader2.rfid.SetTagGroup(CSLibrary.Constants.Selected.ASSERTED, BleMvxApplication._config2.RFID_TagGroup.session, (BleMvxApplication._rfMicro_Target != 1) ? CSLibrary.Constants.SessionTarget.A : CSLibrary.Constants.SessionTarget.B);
            BleMvxApplication._reader2.rfid.SetCurrentSingulationAlgorithm(BleMvxApplication._config2.RFID_Algorithm);
            BleMvxApplication._reader2.rfid.SetCurrentLinkProfile(BleMvxApplication._config2.RFID_Profile);

            // Select RFMicro S3 filter
            {
                CSLibrary.Structures.SelectCriterion extraSlecetion = new CSLibrary.Structures.SelectCriterion();

                extraSlecetion.action = new CSLibrary.Structures.SelectAction(CSLibrary.Constants.Target.SELECTED, CSLibrary.Constants.Action.ASLINVA_DSLINVB, 0);
                extraSlecetion.mask = new CSLibrary.Structures.SelectMask(CSLibrary.Constants.MemoryBank.TID, 0, 28, new byte[] {0xe2, 0x82, 0x40, 0x30});
                BleMvxApplication._reader1.rfid.SetSelectCriteria(0, extraSlecetion);
                BleMvxApplication._reader2.rfid.SetSelectCriteria(0, extraSlecetion);

                // Set OCRSSI Limit
                extraSlecetion.action = new CSLibrary.Structures.SelectAction(CSLibrary.Constants.Target.SELECTED, CSLibrary.Constants.Action.NOTHING_DSLINVB, 0);
                extraSlecetion.mask = new CSLibrary.Structures.SelectMask(CSLibrary.Constants.MemoryBank.BANK3, 0xd0, 8, new byte[] {(byte)(0x20 | BleMvxApplication._rfMicro_minOCRSSI)});
                BleMvxApplication._reader1.rfid.SetSelectCriteria(1, extraSlecetion);
                BleMvxApplication._reader2.rfid.SetSelectCriteria(1, extraSlecetion);

                extraSlecetion.action = new CSLibrary.Structures.SelectAction(CSLibrary.Constants.Target.SELECTED, CSLibrary.Constants.Action.NOTHING_DSLINVB, 0);
                extraSlecetion.mask = new CSLibrary.Structures.SelectMask(CSLibrary.Constants.MemoryBank.BANK3, 0xd0, 8, new byte[] {(byte)(BleMvxApplication._rfMicro_maxOCRSSI)});
                BleMvxApplication._reader1.rfid.SetSelectCriteria(2, extraSlecetion);
                BleMvxApplication._reader2.rfid.SetSelectCriteria(2, extraSlecetion);

                // Temperature and Sensor code
                extraSlecetion.action = new CSLibrary.Structures.SelectAction(CSLibrary.Constants.Target.SELECTED, CSLibrary.Constants.Action.NOTHING_DSLINVB, 0);
                extraSlecetion.mask = new CSLibrary.Structures.SelectMask(CSLibrary.Constants.MemoryBank.BANK3, 0xe0, 0, new byte[] {0x00});
                BleMvxApplication._reader1.rfid.SetSelectCriteria(3, extraSlecetion);
                BleMvxApplication._reader2.rfid.SetSelectCriteria(3, extraSlecetion);

                BleMvxApplication._reader1.rfid.Options.TagRanging.flags |= CSLibrary.Constants.SelectFlags.SELECT;
                BleMvxApplication._reader2.rfid.Options.TagRanging.flags |= CSLibrary.Constants.SelectFlags.SELECT;
            }

            // Multi bank inventory
            BleMvxApplication._reader1.rfid.Options.TagRanging.multibanks = 2;
            BleMvxApplication._reader2.rfid.Options.TagRanging.multibanks = 2;

            BleMvxApplication._reader1.rfid.Options.TagRanging.bank1 = CSLibrary.Constants.MemoryBank.BANK0;
            BleMvxApplication._reader2.rfid.Options.TagRanging.bank1 = CSLibrary.Constants.MemoryBank.BANK0;

            BleMvxApplication._reader1.rfid.Options.TagRanging.offset1 = 12; // Address C
            BleMvxApplication._reader2.rfid.Options.TagRanging.offset1 = 12; // Address C

            BleMvxApplication._reader1.rfid.Options.TagRanging.count1 = 3;
            BleMvxApplication._reader2.rfid.Options.TagRanging.count1 = 3;

            BleMvxApplication._reader1.rfid.Options.TagRanging.bank2 = CSLibrary.Constants.MemoryBank.USER;
            BleMvxApplication._reader2.rfid.Options.TagRanging.bank2 = CSLibrary.Constants.MemoryBank.USER;

            BleMvxApplication._reader1.rfid.Options.TagRanging.offset2 = 8;
            BleMvxApplication._reader1.rfid.Options.TagRanging.count2 = 4;
            BleMvxApplication._reader1.rfid.Options.TagRanging.compactmode = false;

            BleMvxApplication._reader2.rfid.Options.TagRanging.offset2 = 8;
            BleMvxApplication._reader2.rfid.Options.TagRanging.count2 = 4;
            BleMvxApplication._reader2.rfid.Options.TagRanging.compactmode = false;

            BleMvxApplication._reader1.rfid.StartOperation(CSLibrary.Constants.Operation.TAG_PRERANGING);
            BleMvxApplication._reader2.rfid.StartOperation(CSLibrary.Constants.Operation.TAG_PRERANGING);
        }

        void SetConfigPower() {
            if (BleMvxApplication._reader1.rfid.GetAntennaPort() == 1) {
                if (BleMvxApplication._config1.RFID_PowerSequencing_NumberofPower == 0) {
                    BleMvxApplication._reader1.rfid.SetPowerSequencing(0);
                    BleMvxApplication._reader1.rfid.SetPowerLevel(BleMvxApplication._config1.RFID_Antenna_Power[0]);
                }
                else BleMvxApplication._reader1.rfid.SetPowerSequencing(BleMvxApplication._config1.RFID_PowerSequencing_NumberofPower, BleMvxApplication._config1.RFID_PowerSequencing_Level, BleMvxApplication._config1.RFID_PowerSequencing_DWell);
            }
            else {
                for (uint cnt = BleMvxApplication._reader1.rfid.GetAntennaPort() - 1; cnt >= 0; cnt--) {
                    BleMvxApplication._reader1.rfid.SetPowerLevel(BleMvxApplication._config1.RFID_Antenna_Power[cnt], cnt);
                }
            }

            if (BleMvxApplication._reader2.rfid.GetAntennaPort() == 1) {
                if (BleMvxApplication._config2.RFID_PowerSequencing_NumberofPower == 0) {
                    BleMvxApplication._reader2.rfid.SetPowerSequencing(0);
                    BleMvxApplication._reader2.rfid.SetPowerLevel(BleMvxApplication._config2.RFID_Antenna_Power[0]);
                }
                else BleMvxApplication._reader2.rfid.SetPowerSequencing(BleMvxApplication._config2.RFID_PowerSequencing_NumberofPower, BleMvxApplication._config2.RFID_PowerSequencing_Level, BleMvxApplication._config2.RFID_PowerSequencing_DWell);
            }
            else {
                for (uint cnt = BleMvxApplication._reader2.rfid.GetAntennaPort() - 1; cnt >= 0; cnt--) {
                    BleMvxApplication._reader2.rfid.SetPowerLevel(BleMvxApplication._config2.RFID_Antenna_Power[cnt], cnt);
                }
            }
        }

        void SetPower(int index) {
            switch (index) {
                case 0:
                    BleMvxApplication._reader1.rfid.SetPowerSequencing(0);
                    BleMvxApplication._reader2.rfid.SetPowerSequencing(0);
                    BleMvxApplication._reader1.rfid.SetPowerLevel(160);
                    BleMvxApplication._reader2.rfid.SetPowerLevel(160);
                    break;
                case 1:
                    BleMvxApplication._reader1.rfid.SetPowerSequencing(0);
                    BleMvxApplication._reader2.rfid.SetPowerSequencing(0);
                    BleMvxApplication._reader1.rfid.SetPowerLevel(230);
                    BleMvxApplication._reader2.rfid.SetPowerLevel(230);
                    break;
                case 2:
                    BleMvxApplication._reader1.rfid.SetPowerSequencing(0);
                    BleMvxApplication._reader2.rfid.SetPowerSequencing(0);
                    BleMvxApplication._reader1.rfid.SetPowerLevel(300);
                    BleMvxApplication._reader2.rfid.SetPowerLevel(300);
                    break;
                case 3:
                    SetPower(_powerRunning);
                    break;
                case 4:
                    SetConfigPower();
                    break;
            }
        }

        int _powerRunning = 0;
        void StartInventory() {
            if (_startInventory == false) return;
            SetPower(BleMvxApplication._rfMicro_Power);

            {
                _startInventory = false;
                _startInventoryButtonText = "Stop Inventory";
            }

            BleMvxApplication._reader1.rfid.StartOperation(CSLibrary.Constants.Operation.TAG_EXERANGING);
            BleMvxApplication._reader2.rfid.StartOperation(CSLibrary.Constants.Operation.TAG_EXERANGING);

            ClassBattery.SetBatteryMode(ClassBattery.BATTERYMODE.INVENTORY);
            _cancelVoltageValue = true;

            RaisePropertyChanged(() => startInventoryButtonText);
        }

        void StopInventory() {
            _startInventory = true;
            _startInventoryButtonText = "Start Inventory";

            _tagCount = false;

            BleMvxApplication._reader1.rfid.StopOperation();
            BleMvxApplication._reader2.rfid.StopOperation();

            RaisePropertyChanged(() => startInventoryButtonText);

            if (_powerRunning >= 2) _powerRunning = 0;
            else                    _powerRunning++;
        }

        void StartInventoryClick() {
            if (_startInventory) {StartInventory();}
            else                 {StopInventory();}
        }

        void TagInventoryEvent(object sender, CSLibrary.Events.OnAsyncCallbackEventArgs e) {
            if (e.type != CSLibrary.Constants.CallbackType.TAG_RANGING) return;
            if (e.info.Bank1Data == null || e.info.Bank2Data == null) return;

            InvokeOnMainThread(() => {
                AddOrUpdateTagData(e.info);
            });
        }

        void StateChangedEvent1(object sender, CSLibrary.Events.OnStateChangedEventArgs e) {
            switch (e.state) {
                case CSLibrary.Constants.RFState.IDLE:
                    ClassBattery.SetBatteryMode(ClassBattery.BATTERYMODE.IDLE);
                    _cancelVoltageValue = true;
                    switch (BleMvxApplication._reader1.rfid.LastMacErrorCode) {
                        case 0x00:  // normal end
                            break;
                        case 0x0309:
                            _userDialogs.Alert("Too near to metal, please move CS108 away from metal and start inventory again.");
                            break;
                        default:
                            _userDialogs.Alert("Mac error : 0x" + BleMvxApplication._reader1.rfid.LastMacErrorCode.ToString("X4"));
                            break;
                    }
                    break;
            }
        }

        void StateChangedEvent2(object sender, CSLibrary.Events.OnStateChangedEventArgs e) {
            switch (e.state) {
                case CSLibrary.Constants.RFState.IDLE:
                    ClassBattery.SetBatteryMode(ClassBattery.BATTERYMODE.IDLE);
                    _cancelVoltageValue = true;
                    switch (BleMvxApplication._reader2.rfid.LastMacErrorCode) {
                        case 0x00:  // normal end
                            break;
                        case 0x0309:    // 
                            _userDialogs.Alert("Too near to metal, please move CS108 away from metal and start inventory again.");
                            break;
                        default:
                            _userDialogs.Alert("Mac error : 0x" + BleMvxApplication._reader2.rfid.LastMacErrorCode.ToString("X4"));
                            break;
                    }
                    break;
            }
        }

        private void AddOrUpdateTagData(CSLibrary.Structures.TagCallbackInfo info) {
            InvokeOnMainThread(() => {
                bool found = false;
                int cnt;

                lock (TagInfoList) {
                    UInt16 sensorCode = (UInt16)(info.Bank1Data[0] & 0x1ff); // address c
                    UInt16 ocRSSI     = info.Bank1Data[1];                   // address d
                    UInt16 temp       = info.Bank1Data[2];                   // address e

                    for (cnt = 0; cnt < TagInfoList.Count; cnt++) {
                        if (epcs.Contains(info.epc.ToString()) && (TagInfoList[cnt].EPC == info.epc.ToString())) {
                            TagInfoList[cnt].OCRSSI    = ocRSSI;
                            TagInfoList[cnt].RSSIColor = "Black";

                            if (ocRSSI >= BleMvxApplication._rfMicro_minOCRSSI && ocRSSI <= BleMvxApplication._rfMicro_maxOCRSSI) {
                                // BleMvxApplication._rfMicro_SensorType // 0 = Sensor code, 1 = Temp
                                // BleMvxApplication._rfMicro_SensorUnit // 0 = code, 1 = f, 2 = c, 3 = %

                                switch (BleMvxApplication._rfMicro_SensorType) {
                                    case 0: break;

                                    default:
                                        if (temp >= 1300 && temp <= 3500) {
                                            double SensorAvgValue;
                                            TagInfoList[cnt].SucessCount++;
                                            UInt64 caldata = (UInt64)(((UInt64)info.Bank2Data[0] << 48) | ((UInt64)info.Bank2Data[1] << 32) | ((UInt64)info.Bank2Data[2] << 16) | ((UInt64)info.Bank2Data[3]));

                                            if (caldata == 0) {TagInfoList[cnt].SensorAvgValue = "NoCalData";}
                                            else {
                                                switch (BleMvxApplication._rfMicro_SensorUnit) {
                                                    case 0: // Code
                                                        TagInfoList[cnt]._sensorValueSum += temp;
                                                        SensorAvgValue = Math.Round(TagInfoList[cnt]._sensorValueSum / TagInfoList[cnt].SucessCount, 2);
                                                        TagInfoList[cnt].SensorAvgValue = SensorAvgValue.ToString();
                                                        break;
                                                    case 2: // F
                                                        TagInfoList[cnt]._sensorValueSum += getTempF(temp, caldata);
                                                        SensorAvgValue = Math.Round(TagInfoList[cnt]._sensorValueSum / TagInfoList[cnt].SucessCount, 2);
                                                        TagInfoList[cnt].SensorAvgValue = SensorAvgValue.ToString();
                                                        break;
                                                    default: // C
                                                        double SAV = Math.Round(getTempC(temp, caldata), 2);                                               
                                                        TagInfoList[cnt].SensorAvgValue = SAV.ToString();
                                                        TagInfoList[cnt].TimeString = DateTime.Now.ToString("HH:mm:ss");

                                                        try {
                                                            if (!tag_List.Contains(TagInfoList[cnt].EPC)) { // Check Tag_List contains tags, add new data
                                                                tag_List.Add(TagInfoList[cnt].EPC);
                                                            }

                                                            if (!tag_Time.ContainsKey(TagInfoList[cnt].EPC)) { // Check Tag_Time contains tags, add new data
                                                                List<string> t_time = new List<string>{TagInfoList[cnt].TimeString};
                                                                tag_Time.Add(TagInfoList[cnt].EPC, t_time);
                                                            }
                                                            else {
                                                                tag_Time[TagInfoList[cnt].EPC].Add(TagInfoList[cnt].TimeString);
                                                            }

                                                            if (!tag_Data.ContainsKey(TagInfoList[cnt].EPC)) { // Check Tag_Data contains tags, add new data
                                                                List<string> t_data = new List<string>{TagInfoList[cnt].SensorAvgValue};
                                                                tag_Data.Add(TagInfoList[cnt].EPC, t_data);
                                                            }
                                                            else {
                                                                tag_Data[TagInfoList[cnt].EPC].Add(TagInfoList[cnt].SensorAvgValue);
                                                            }

                                                            if (!tag_RSSI.ContainsKey(TagInfoList[cnt].EPC)) {
                                                                List<string> t_rssi = new List<string>{TagInfoList[cnt].OCRSSI.ToString()};
                                                                tag_RSSI.Add(TagInfoList[cnt].EPC, t_rssi);
                                                            }
                                                            else {
                                                                tag_RSSI[TagInfoList[cnt].EPC].Add(TagInfoList[cnt].OCRSSI.ToString());
                                                            }
                                                        }

                                                        finally {
                                                            switch (TagInfoList[cnt].EPC) {

                                                                case "E282403E000207D6F9779863": // Left Calf
                                                                    _LeftCalfTemp = TagInfoList[cnt].SensorAvgValue + "°";
                                                                    RaisePropertyChanged(() => LeftCalfTemp);

                                                                    if ((SAV>20) && (_LeftCalf!="green")) {
                                                                        _LeftCalf = "green";
                                                                        RaisePropertyChanged(() => LeftCalf);
                                                                    }
                                                                    else if ((SAV<=20) && (_LeftCalf!="red")) {
                                                                        _LeftCalf = "red";
                                                                        RaisePropertyChanged(() => LeftCalf);
                                                                    } 
                                                                    break;

                                                                case "E282403E000207D6F9775A25": // Right Calf
                                                                        _RightCalfTemp = TagInfoList[cnt].SensorAvgValue + "°";
                                                                        RaisePropertyChanged(() => RightCalfTemp);

                                                                        if ((SAV>20) && (_RightCalf!="green")) {
                                                                            _RightCalf = "green";
                                                                            RaisePropertyChanged(() => RightCalf);
                                                                        }
                                                                        else if ((SAV<=20) && (_RightCalf!="red")) {
                                                                            _RightCalf = "red";
                                                                            RaisePropertyChanged(() => RightCalf);
                                                                        } 
                                                                        break;

                                                                case "E282403E000207D6F9770273": // Left Quad 
                                                                    _LeftQuadTemp = TagInfoList[cnt].SensorAvgValue + "°";
                                                                    RaisePropertyChanged(() => LeftQuadTemp);

                                                                    if ((SAV>20) && (_LeftQuad!="green")) {
                                                                        _LeftQuad = "green";
                                                                        RaisePropertyChanged(() => LeftQuad);
                                                                    }
                                                                    else if ((SAV<=20) && (_LeftQuad!="red")) {
                                                                        _LeftQuad = "red";
                                                                        RaisePropertyChanged(() => LeftQuad);
                                                                    } 
                                                                    break;

                                                                case "E282403E000207D6F97786D2": // Right Quad
                                                                    _RightQuadTemp = TagInfoList[cnt].SensorAvgValue + "°";
                                                                    RaisePropertyChanged(() => RightQuadTemp);

                                                                    if ((SAV>20) && (_RightQuad!="green")) {
                                                                        _RightQuad = "green";
                                                                        RaisePropertyChanged(() => RightQuad);
                                                                    }
                                                                    else if ((SAV<=20) && (_RightQuad!="red")) {
                                                                        _RightQuad = "red";
                                                                        RaisePropertyChanged(() => RightQuad);
                                                                    } 
                                                                    break;

                                                                case "E282403E000207D6F97748C0": // Right Glute
                                                                    _RightGluteTemp = TagInfoList[cnt].SensorAvgValue + "°";
                                                                    RaisePropertyChanged(() => RightGluteTemp);

                                                                    if ((SAV>20) && (_RightGlute!="green")) {
                                                                        _RightGlute = "green";
                                                                        RaisePropertyChanged(() => RightGlute);
                                                                    }
                                                                    else if ((SAV<=20) && (_RightGlute!="red")) {
                                                                        _RightGlute = "red";
                                                                        RaisePropertyChanged(() => RightGlute);
                                                                    } 
                                                                    break;

                                                                case "E282403E000207D6F9773098": // Left Glute
                                                                    _LeftGluteTemp = TagInfoList[cnt].SensorAvgValue + "°";
                                                                    RaisePropertyChanged(() => LeftGluteTemp);

                                                                    if ((SAV>20) && (_LeftGlute!="green")) {
                                                                        _LeftGlute = "green";
                                                                        RaisePropertyChanged(() => LeftGlute);
                                                                    }
                                                                    else if ((SAV<=20) && (_LeftGlute!="red")) {
                                                                        _LeftGlute = "red";
                                                                        RaisePropertyChanged(() => LeftGlute);
                                                                    } 
                                                                    break;

                                                                default: 
                                                                    break; 
                                                                }
                                                            }
                                                        break;
                                                }
                                            }
                                        }
                                        break;
                                }
                            }
                            else {
                                TagInfoList[cnt].RSSIColor = "Red";
                            }
                            found = true;
                            break;
                        }
                    }

                    if (!found) {
                        if (epcs.Contains(info.epc.ToString())) {
                            RFMicroTagInfoViewModel item = new RFMicroTagInfoViewModel();

                            item.EPC = info.epc.ToString();
                            item.NickName = GetNickName(item.EPC);
                            if (item.NickName != "") item.DisplayName = item.NickName;
                            else                     item.DisplayName = item.EPC;

                            item.OCRSSI = ocRSSI;
                            item.SucessCount = 0;
                            item._sensorValueSum = 0;
                            item.SensorAvgValue = "";
                            item.RSSIColor = "Black";
                            item.SAV = 0;
                            item.Performance = "";

                            if (ocRSSI >= BleMvxApplication._rfMicro_minOCRSSI && ocRSSI <= BleMvxApplication._rfMicro_maxOCRSSI) {
                                // BleMvxApplication._rfMicro_SensorType // 0 = Sensor code, 1 = Temp
                                // BleMvxApplication._rfMicro_SensorUnit // 0 = code, 1 = f, 2 = c, 3 = %

                                switch (BleMvxApplication._rfMicro_SensorType) {
                                    case 0:
                                        break;

                                    default:
                                        if (temp >= 1300 && temp <= 3500) {
                                            item.SucessCount++;
                                            UInt64 caldata = (UInt64)(((UInt64)info.Bank2Data[0] << 48) | ((UInt64)info.Bank2Data[1] << 32) | ((UInt64)info.Bank2Data[2] << 16) | ((UInt64)info.Bank2Data[3]));

                                            if (caldata == 0) item.SensorAvgValue = "NoCalData";
                                            else
                                                switch (BleMvxApplication._rfMicro_SensorUnit) {
                                                    case 0: // code
                                                        item._sensorValueSum = temp;
                                                        item.SensorAvgValue = item._sensorValueSum.ToString();
                                                        break;
                                                    case 2: // F
                                                        item._sensorValueSum = getTempF(temp, caldata);
                                                        item.SensorAvgValue = Math.Round(item._sensorValueSum, 2).ToString();
                                                        break;
                                                    default: // C
                                                        double SAV = Math.Round(getTempC(temp, caldata), 2);   
                                                        item.SensorAvgValue = SAV.ToString();
                                                        item.TimeString = DateTime.Now.ToString("HH:mm:ss");

                                                        if (epcs.Contains(item.EPC)) {
                                                            List<string> t_time = new List<string>{item.TimeString};
                                                            List<string> t_data = new List<string>{item.SensorAvgValue};
                                                            List<string> t_rssi = new List<string>{item.OCRSSI.ToString()};

                                                            try {
                                                                tag_Time.Add(item.EPC, t_time);
                                                                tag_Data.Add(item.EPC, t_data);
                                                                tag_RSSI.Add(item.EPC, t_rssi);
                                                                tag_List.Add(item.EPC);
                                                            }
                                                            finally {}
                                                        }
                                                        break;
                                                }
                                        }
                                        break;
                                }
                            }
                            else {item.RSSIColor = "Red";}
                            
                            TagInfoList.Insert(0, item);
                            Trace.Message("EPC Data = {0}", item.EPC);
                        }
                    }
                }
            });
        }

        string GetNickName(string EPC) {
            for (int index = 0; index < ViewModelRFMicroNickname._TagNicknameList.Count; index++)
                if (ViewModelRFMicroNickname._TagNicknameList[index].EPC == EPC)
                    return ViewModelRFMicroNickname._TagNicknameList[index].Nickname;
            return "";
        }

        double getTempF(UInt16 temp, UInt64 CalCode) {
            return (getTemperatue(temp, CalCode) * 1.8 + 32.0);
        }

        double getTempC(UInt16 temp, UInt64 CalCode) {
            return getTemperatue(temp, CalCode);
        }

        double getTemperatue(UInt16 temp, UInt64 CalCode) {
            int crc = (int)(CalCode >> 48) & 0xffff;
            int calCode1 = (int)(CalCode >> 36) & 0x0fff;
            int calTemp1 = (int)(CalCode >> 25) & 0x07ff;
            int calCode2 = (int)(CalCode >> 13) & 0x0fff;
            int calTemp2 = (int)(CalCode >> 2) & 0x7FF;
            int calVer = (int)(CalCode & 0x03);

            double fTemperature = temp;
            fTemperature = ((double)calTemp2 - (double)calTemp1) * (fTemperature - (double)calCode1);
            fTemperature /= ((double)(calCode2) - (double)calCode1);
            fTemperature += (double)calTemp1;
            fTemperature -= 800;
            fTemperature /= 10;
            return fTemperature;
        }

        void VoltageEvent1(object sender, CSLibrary.Notification.VoltageEventArgs e) {
            if (e.Voltage == 0xffff) {
                _labelVoltage = "CS108 Bat. ERROR";    // 3.98v
            }
            else { // to fix CS108 voltage bug
                if (_cancelVoltageValue) {
                    _cancelVoltageValue = false;
                    return;
                }

                switch (BleMvxApplication._config1.BatteryLevelIndicatorFormat) {
                    case 0:
                        _labelVoltage = "CS108 Bat. " + ((double)e.Voltage / 1000).ToString("0.000") + "v"; //			v
                        break;
                    default:
                        _labelVoltage = "CS108 Bat. " + ClassBattery.Voltage2Percent((double)e.Voltage / 1000).ToString("0") + "%"; //			%
                        break;
                }
            }
			RaisePropertyChanged(() => labelVoltage);
		}

        void VoltageEvent2(object sender, CSLibrary.Notification.VoltageEventArgs e) {
            if (e.Voltage == 0xffff) {
                _labelVoltage = "CS108 Bat. ERROR";    // 3.98v
            }
            else {
                // to fix CS108 voltage bug
                if (_cancelVoltageValue) {
                    _cancelVoltageValue = false;
                    return;
                }

                switch (BleMvxApplication._config2.BatteryLevelIndicatorFormat) {
                    case 0:
                        _labelVoltage = "CS108 Bat. " + ((double)e.Voltage / 1000).ToString("0.000") + "v"; // v
                        break;

                    default:
                        _labelVoltage = "CS108 Bat. " + ClassBattery.Voltage2Percent((double)e.Voltage / 1000).ToString("0") + "%"; // %
                        break;
                }
            }
			RaisePropertyChanged(() => labelVoltage);
		}

        private void ShareDataButtonClick() { // Function for Sharing time series data from tags
            InvokeOnMainThread(() => {
                string fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "tags.csv");
                var myExport = new CsvExport();
                var maxValue = tag_Data.Max(t => t.Value.Count); 

                for (var i = 0; i < maxValue; i++) {
                    myExport.AddRow();

                    foreach (string name in tag_List) {
                        // try {
                        //     myExport[name] = "(" + tag_Time[name][i] + "," + tag_Data[name][i] + ")"; 
                        // }
                        try {
                            myExport[map[name]] = tag_Time[name][i] + "," + tag_Data[name][i] + "," + tag_RSSI[name][i]; 
                        }
                        catch {}
                    }
                }
                myExport.ExportToFile(fileName);
            });
        }

        #region Key_event
        void HotKeys_OnKeyEvent(object sender, CSLibrary.Notification.HotKeyEventArgs e) {
            if (e.KeyCode == CSLibrary.Notification.Key.BUTTON) {
                if (e.KeyDown)  {StartInventory();}
                else            {StopInventory();}
            }
        }
        #endregion

        async void ShowDialog(string Msg) {
            var config = new ProgressDialogConfig() {
                Title = Msg,
                IsDeterministic = true,
                MaskType = MaskType.Gradient,
            };

            using (var progress = _userDialogs.Progress(config)) {
                progress.Show();
                await System.Threading.Tasks.Task.Delay(1000);
            }
        }

    }
}
    
