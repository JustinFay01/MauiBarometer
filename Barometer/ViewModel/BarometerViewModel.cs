using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Diagnostics; //Debug
using System.IO.Ports;


//Example of Serial Connection with C#
//https://www.c-sharpcorner.com/uploadfile/eclipsed4utoo/communicating-with-serial-port-in-C-Sharp/
//Link to docs
//https://learn.microsoft.com/en-us/dotnet/api/system.io.ports.serialport?view=netframework-4.8

namespace Barometer.ViewModel {
    public partial class BarometerViewModel : ObservableObject {

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        bool isBusy;

        [ObservableProperty]
        bool isConnected;

        [ObservableProperty]
        string title;

        public ObservableCollection<string> Values { get; set; } = new();

        public bool IsNotConnected => !IsConnected;
        public bool IsNotBusy => !IsBusy;

        SerialPortController serialPortController;
        public BarometerViewModel() {
            //UI
            Title = "Status";
            serialPortController = new SerialPortController("COM5", 9600, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);

            //On startup
            _ = OpenAsync();
           
        }

        [RelayCommand]
        async Task RunReadAndWriteAsync() {
            while(IsConnected) {
                var incoming = await serialPortController.ReadAndWrite("p", CancellationToken.None);

                if(incoming is null) {
                    await Shell.Current.DisplayAlert("Error!", $" Null value read", "OK");
                }

                if(Values.Count < 6) {
                    Values.Add(incoming);
                }
                else {
                    Values.RemoveAt(0);
                    Values.Add(incoming);
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
                
                
                _ = RunReadAndWriteAsync();
                await Shell.Current.DisplayAlert("Attempted Connection", $"Connection status is {IsConnected}", "OK");
            }
        }

        [RelayCommand]
        async Task CloseAsync() {
            try {
                var cancellationTokenSource = new CancellationTokenSource();
                await serialPortController.ReadAndWrite("p", cancellationTokenSource.Token);


                serialPortController.ClosePort();
            }
            catch (Exception ex) {
                Debug.Write(ex);
                await Shell.Current.DisplayAlert("Error!", $"Unable to close: {ex.Message}", "OK");
            }
            finally {
                IsConnected = serialPortController.GetStatus();
                IsBusy = false;
                await Shell.Current.DisplayAlert("Closing Connection", $"Connection status is {IsConnected}", "OK");
            }
        }
    }
}
