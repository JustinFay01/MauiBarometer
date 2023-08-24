using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Diagnostics; //Debug


//Example of Serial Connection with C#
//https://www.c-sharpcorner.com/uploadfile/eclipsed4utoo/communicating-with-serial-port-in-C-Sharp/
//Link to docs
//https://learn.microsoft.com/en-us/dotnet/api/system.io.ports.serialport?view=netframework-4.8

namespace Barometer.ViewModel {
    public partial class BarometerViewModel : ObservableObject {

        //UI Fields
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        bool isBusy;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotConnected))]
        bool isConnected;

        [ObservableProperty]
        string title;

        public ObservableCollection<string> Values { get; set; } = new();

        public bool IsNotConnected => !IsConnected;
        public bool IsNotBusy => !IsBusy;


        //Internal Fields
        int MAX_READINGS = 6;
        SerialPortController serialPortController;
        bool closeExpected = false;
        string logPath;

        public BarometerViewModel() {
            try {
                IsBusy = true;
                //UI
                Title = "Barometeric Pressure";
                serialPortController = new SerialPortController("COM5", 9600, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);
            }
            catch (Exception ex) {
                Debug.Write(ex);
            }
            finally {
                IsBusy = false;
            }

        }
         
        async Task StartUpAsync(CancellationToken cancellationToken) {
            var openConTask = OpenAsync();
            var checkConTask = CheckConnectionAsync(cancellationToken);
            //check if a folder was selected prefs
            if (!Preferences.ContainsKey("LogPath")) {
                var result = PickFolderAsync(CancellationToken.None);
                await result.ConfigureAwait(false);

                Preferences.Set("LogPath", result.ToString());
                await Shell.Current.DisplayAlert("Test", $"{Preferences.Get("LogPath", "null")}", "OK");
            }
            else {
                logPath = Preferences.Get("LogPath", System.AppContext.BaseDirectory.ToString());
            }

            await openConTask;
            await checkConTask;
        }

        [RelayCommand]
        async Task PickFolderAsync(CancellationToken cancellationToken) {
            try {
                var result = FolderPicker.Default.PickAsync(cancellationToken);
                FolderPickerResult folder = await result;
                await Shell.Current.DisplayAlert("Folder Selected", $"Previous Folder {Preferences.Get("LogPath", "null")}\nNew Folder {folder.Folder.Path}", "OK");
                Preferences.Set("LogPath", folder.Folder.Path);
            }
            catch (Exception ex) {
                Debug.Write(ex);
                await Shell.Current.DisplayAlert("Error!", $"Unable to Select Folder: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        async Task CheckConnectionAsync(CancellationToken cancellationToken) {
            while (!cancellationToken.IsCancellationRequested) {
                IsConnected = serialPortController.GetStatus();
                if (!IsConnected && !closeExpected) {
                    _ = OpenAsync();
                }
                await Task.Delay(500);
            }
        }

        [RelayCommand]
        async Task RunReadAndWriteAsync(CancellationToken cancellationToken) {
            while (IsConnected && cancellationToken == CancellationToken.None) {
                var incoming = await serialPortController.ReadAndWrite("p");

                if (incoming is null || incoming == "") {
                    await Shell.Current.DisplayAlert("Error!", $" Null value read", "OK");
                }

                if (Values.Count < MAX_READINGS) {
                    Values.Insert(0, incoming);
                }
                else {
                    Values.RemoveAt(MAX_READINGS - 1);
                    Values.Insert(0, incoming);
                }
            }
        }

        [RelayCommand]
        async Task OpenAsync() {
            //hard coded defualts for now
            try {
                //Cancel async task
                serialPortController.OpenPort();
                IsBusy = true;
                //Makes button change color
                await Task.Delay(100);
            }
            catch (Exception ex) {
                Debug.Write(ex);
                await Shell.Current.DisplayAlert("Error!", $"Unable to open connection: {ex.Message}", "OK");
            }
            finally {
                IsConnected = serialPortController.GetStatus();
                IsBusy = false;
                closeExpected = false;

                //Discard result (exception caught in its own method) and do this on different thread
                _ = RunReadAndWriteAsync(CancellationToken.None).ConfigureAwait(false);
               
            }
        }

        [RelayCommand]
        async Task CloseAsync() {
            try {
                IsBusy = true;
                //Makes button change color
                await Task.Delay(100);
                //Cancel the async ReadAndWrite Task before closing connection
                //Discard result and do this on different thread
                _ = RunReadAndWriteAsync(new CancellationTokenSource().Token).ConfigureAwait(false);
                serialPortController.ClosePort();
            }
            catch (Exception ex) {
                Debug.Write(ex);
                await Shell.Current.DisplayAlert("Error!", $"Unable to close: {ex.Message}", "OK");

            }
            finally {
                IsConnected = serialPortController.GetStatus();
                IsBusy = false;
                closeExpected = true;
            }
        }
    }
}
