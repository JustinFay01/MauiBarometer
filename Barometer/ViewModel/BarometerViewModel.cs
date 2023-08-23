using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics; //Debug
using System.IO.Ports;
using System.Runtime.CompilerServices;
using System.Threading;
using static System.Runtime.InteropServices.JavaScript.JSType;


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
            //UI
            Title = "Barometeric Pressure";
            serialPortController = new SerialPortController("COM5", 9600, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);

            //On startup _ = makes it so the task results are thrown away and we dont have to wait for them
            _ = StartUpAsync(CancellationToken.None);
        }

        async Task StartUpAsync(CancellationToken cancellationToken) {
            //check if a folder was selected prefs
            Shell.Current.DisplayAlert("Log Path Not Found", $"{Preferences.ContainsKey("LogPath")}, {Preferences.Get("LogPath", "Not found")}", "OK");
            if (!Preferences.ContainsKey("LogPath")) {
                Shell.Current.DisplayAlert("Log Path Not Found", "Please select a folder to store log files", "OK");
                var result = PickFolderAsync(CancellationToken.None);
                Preferences.Set("LogPath", result.ToString());
            }
            else {
                logPath = Preferences.Get("LogPath", "");
            }


            OpenAsync();
            CheckConnectionAsync(cancellationToken);
        }
        
        async Task PickFolderAsync(CancellationToken cancellationToken) {
            try {
                var result = await FolderPicker.Default.PickAsync(cancellationToken);
                result.ToString();
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
                if(!IsConnected && !closeExpected) {
                    await Shell.Current.DisplayAlert("Unexpected Disconnet", $"Attempting to reconnect...", "OK");
                    _ = OpenAsync();
                }
                await Task.Delay(500);
            }
        }

        [RelayCommand]
        async Task RunReadAndWriteAsync() {
            while(IsConnected) {
                var incoming = await serialPortController.ReadAndWrite("p", CancellationToken.None);
              
                if (incoming == "") {
                    await Shell.Current.DisplayAlert("Error!", $" Null value read", "OK");
                }

                if(Values.Count < MAX_READINGS) {
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
            }
            catch (Exception ex) {
                Debug.Write(ex);
                await Shell.Current.DisplayAlert("Error!", $"Unable to open connection: {ex.Message}", "OK");
            }
            finally {
                IsConnected = serialPortController.GetStatus();
                IsBusy = false;
                closeExpected = false;

                _ = RunReadAndWriteAsync();
            }
        }

        [RelayCommand]
        async Task CloseAsync() {
            try {
                //Cancel the async ReadAndWrite Task before closing connection
                await serialPortController.ReadAndWrite("", new CancellationTokenSource().Token);
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
