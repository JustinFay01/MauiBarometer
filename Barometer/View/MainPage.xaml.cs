using Barometer.ViewModel;


namespace Barometer {
    public partial class MainPage : ContentPage {

        public MainPage(BarometerViewModel viewModel) {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }

}
