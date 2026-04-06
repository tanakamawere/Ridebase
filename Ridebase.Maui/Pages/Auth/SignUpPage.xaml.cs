using Ridebase.ViewModels;

namespace Ridebase.Pages.Auth;

public partial class SignUpPage : ContentPage
{
    public SignUpPage(SignUpViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
