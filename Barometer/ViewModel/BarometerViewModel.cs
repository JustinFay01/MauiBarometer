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
        int timeDelay = 1;
        bool WriteLogs = true;
        SerialPortController serialPortController;
        bool closeExpected = false;
        string logPath;

        public BarometerViewModel() {
            try {
                IsBusy = true;
                //UI
                Title = "Barometeric Pressure";
                serialPortController = new SerialPortController("COM5", 9600, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);
                if(Preferences.Get("LogPath", "null").Equals("null")) {
                    WriteLogs = false;
                }
                _ = OpenAsync();
            }
            catch (Exception ex) {
                Debug.Write(ex);
            }
            finally {
                IsBusy = false;
            }

        }

        [RelayCommand]
        async Task ClearFolderAsync() {
            Preferences.Clear();
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
                Preferences.Set("LogPath", folder.Folder.Path);
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
                            await RunReadAndWriteAsync(WriteLogs, CancellationToken.None);
                        }
                    }
                }
                await Task.Delay(500);
            }
        }

        [RelayCommand]
        async Task RunReadAndWriteAsync(bool WriteLogs, CancellationToken cancellationToken) {
            while (IsConnected && cancellationToken == CancellationToken.None) {
                var incoming = await serialPortController.ReadAndWrite("p", 1);

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
                _ = RunReadAndWriteAsync(WriteLogs, CancellationToken.None).ConfigureAwait(false);
                _ = CheckConnectionAsync(CancellationToken.None).ConfigureAwait(false);
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
                _ = RunReadAndWriteAsync(WriteLogs, new CancellationTokenSource().Token).ConfigureAwait(false);
                _ = CheckConnectionAsync(new CancellationTokenSource().Token).ConfigureAwait(false);

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
