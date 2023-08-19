using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Barometer.Services;
using System.Diagnostics; //Debug

namespace Barometer.ViewModel {
    public partial class BarometerViewModel : ObservableObject {

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        bool isBusy;

        [ObservableProperty]
        bool isConnected;

        [ObservableProperty]
        string title;
        public bool IsNotConnected => !IsConnected;
        public bool IsNotBusy => !IsBusy;

        SerialPortController serialPortController;

        public BarometerViewModel() {
            Title = "Status";
        }

        [RelayCommand]
        async Task OpenAsync() {
            //hard coded defualts for now
            /* dcbParams.BaudRate = CBR_9600;
            dcbParams.ByteSize = 8;
            dcbParams.StopBits = ONESTOPBIT;
            dcbParams.Parity = NOPARITY;
            dcbParams.fDtrControl = DTR_CONTROL_ENABLE;*/
            /*if(serialPortController is null) {
                return;
            }*/

            try {
                serialPortController = new("COM5", 9600, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);
                IsBusy = true;
            }
            catch (Exception ex) {
                Debug.Write(ex);
                await Shell.Current.DisplayAlert("Error!", $"Unable to open connection: {ex.Message}", "OK");
            }
            finally {
                IsConnected = serialPortController.GetStatus();
                IsBusy = false;
                await Shell.Current.DisplayAlert("Attempted Connection", $"Connection status is {IsConnected}", "OK");
            }
            //IsConnected = sp.OpenConnection();
            
        }

        [RelayCommand]
        async Task CloseAsync() {
            try {

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
