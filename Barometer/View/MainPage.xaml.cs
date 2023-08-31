using Barometer.ViewModel;


namespace Barometer {
    public partial class MainPage : ContentPage {

        BarometerViewModel viewModel;
        public MainPage(BarometerViewModel viewModel) {
            this.viewModel = viewModel;

            InitializeComponent();
            BindingContext = viewModel;
        }

        async void OnCheckBoxCheckedChanged(object sender, CheckedChangedEventArgs e) {
            // Perform required operation after examining e.Value
            try {
                await viewModel?.CheckBoxHandlerAsync(sender, e);
            } catch (Exception ex) {
                await Shell.Current.DisplayAlert("Error!", $"{ex.Message}", "OK");
            }
        }
    }

}
