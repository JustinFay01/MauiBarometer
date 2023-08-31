using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Diagnostics; //Debug
using System.Text.RegularExpressions;


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

        [ObservableProperty]
        bool writeLogs;

        public ObservableCollection<string> Values { get; set; } = new();

        public bool IsNotWriting => !WriteLogs;
        public bool IsNotConnected => !IsConnected;
        public bool IsNotBusy => !IsBusy;


        //Internal Fields
        int MAX_READINGS = 6;
        int timeDelay = 5;
        SerialPortController serialPortController;
        bool closeExpected = false;
        string logPath;
        string logFolderName = "BarometerLogs";
        LogWriter logWriter;

        //Cancelation Tokens for each method?
        CancellationTokenSource CheckConnectionToken;
        CancellationTokenSource ReadAndWriteToken;

        public BarometerViewModel() {
            try {
                CheckConnectionToken = new CancellationTokenSource();
                ReadAndWriteToken = new CancellationTokenSource();

                IsBusy = true;
                //UI
                Title = "Barometeric Pressure";
                serialPortController = new SerialPortController("COM5", 9600, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);

                logPath = Preferences.Get("LogPath", "null");
                if (logPath.Equals("null")) {
                    WriteLogs = false;
                    logPath = System.AppContext.BaseDirectory.ToString();
                    Preferences.Set("LogPath", System.AppContext.BaseDirectory.ToString());
                }
                else {
                    WriteLogs = true;
                }
      
                logWriter = new LogWriter(logFolderName, logPath);
                _ = OpenAsync();
            }
            catch (Exception ex) {
                Debug.Write(ex);
            }
            finally {
                IsBusy = false;
            }

        }

        //Event handler for code behind of MainPage
        public async Task CheckBoxHandlerAsync(object sender, CheckedChangedEventArgs e) {
            try {
                if (e.Value == serialPortController.GetNewLine()) {
                    return;
                }

                //Cancel the current read and write command
                ReadAndWriteToken.Cancel();
                //Then change set bool field in serial port to be writing with or without new line
                serialPortController.SetNewLine(e.Value);
                //Then restart read and write process
                await RunReadAndWriteAsync(WriteLogs, ReadAndWriteToken.Token);
            }
            catch (Exception ex) {
                Debug.Write(ex);
                await Shell.Current.DisplayAlert("Error!", $"Error Handling Checkbox: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        async Task ClearFolderAsync() {
            Preferences.Clear();
            WriteLogs = false;
            await Shell.Current.DisplayAlert("Clearing Folders", $"Folder has been cleared, default path is {System.AppContext.BaseDirectory}", "OK");
        }

        [RelayCommand]
        async Task PickFolderAsync(CancellationToken cancellationToken) {
            try {
                var result = FolderPicker.Default.PickAsync(cancellationToken);
                FolderPickerResult folder = await result;
                if (folder?.Folder is null)
                    return;

                await Shell.Current.DisplayAlert("Folder Selected", $"Previous Folder {Preferences.Get("LogPath", "null")}\nNew Folder {folder.Folder?.Path}", "OK");
                Preferences.Set("LogPath", folder.Folder?.Path);
                WriteLogs = true;
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
                    try {
                        serialPortController.OpenPort();
                    }
                    catch (FileNotFoundException ex){
                        Debug.Write(ex);
                    }
                    catch (Exception ex) {
                        await Shell.Current.DisplayAlert("Error!", $"Unable to open connection: {ex.Message}", "OK");
                    }
                    finally {
                        if (serialPortController.GetStatus()) {
                            //This is not running correctly upon automatic restart
                            
                        }
                    }
                }
                else { // Otherwise we are connected
                    //Check time and write log if necessary
                }
                await Task.Delay(500);
            }
        }

        [RelayCommand]
        async Task RunReadAndWriteAsync(bool WriteLogs, CancellationToken cancellationToken) {
            //Wait until time rounds to timeDelay
            await Task.Delay((int) ((60f - DateTime.Now.Second) % timeDelay) * 1000);

            while (IsConnected && !cancellationToken.IsCancellationRequested) {
                string incoming = "";
                try {
                    incoming = await serialPortController.ReadAndWrite($"P+30.3117 in Hg A SEA-LEVEL OK {DateTime.Now}", timeDelay);
                }
                catch (Exception ex) {
                    Debug.Write(ex);
                    await Shell.Current.DisplayAlert("Error!", $"Problem Reading or Writing to serial port: {ex.Message}", "OK");
                    continue;
                }

                if (incoming is null || incoming == "") {
                    await Shell.Current.DisplayAlert("Error!", $" Null value read clearing port", "OK");
                    continue;
                }

                if (Values.Count < MAX_READINGS) {
                    Values.Insert(0, incoming);
                }
                else {
                    Values.RemoveAt(MAX_READINGS - 1);
                    Values.Insert(0, incoming);
                }

                if(WriteLogs) {
                    try {
                        Match m = Regex.Match(incoming, @"P\+([0-9]+.[0-9]+)", RegexOptions.None);
                        string value = $"{DateTime.Now.ToString("HH:mm:sstt")}, {m?.Groups[1]?.ToString()}\n";
                        logWriter.WriteToFile($"{DateTime.Now.ToString("MMddyyyy")}.txt", value);
                    }
                    catch (Exception ex) {
                        Debug.Write(ex);
                        await Shell.Current.DisplayAlert("Error!", $"Unable to write to log file: {ex.Message}", "OK");
                    }
                }

                if(cancellationToken.IsCancellationRequested) {
                    await Shell.Current.DisplayAlert("Ending!", "yay!", "OK");
                }
            }
        }

        [RelayCommand]
        async Task OpenAsync() {
            //hard coded defualts for now
            try {
                if (serialPortController.GetStatus()) {
                    return;
                }
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
                await RunReadAndWriteAsync(WriteLogs, ReadAndWriteToken.Token);
                await CheckConnectionAsync(CheckConnectionToken.Token);
            }
        }

        [RelayCommand]
        async Task CloseAsync() {
            try {
                IsBusy = true;
                closeExpected = true;
                //Makes button change color
                //await Task.Delay(100);

                //Cancel the async ReadAndWrite Task before closing connection
                //Discard result and do this on different thread
                ReadAndWriteToken.Cancel();
                CheckConnectionToken.Cancel();

                ReadAndWriteToken.TryReset();
                CheckConnectionToken.TryReset();

                serialPortController.ClosePort();
            }
            catch (Exception ex) {
                Debug.Write(ex);
                await Shell.Current.DisplayAlert("Error!", $"Unable to close: {ex.Message}", "OK");

            }
            finally {
                IsConnected = serialPortController.GetStatus();
                IsBusy = false;
                closeExpected = false;
            }
        }
    }
}
