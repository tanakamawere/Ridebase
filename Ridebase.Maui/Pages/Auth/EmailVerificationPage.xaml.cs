using Ridebase.ViewModels;

namespace Ridebase.Pages.Auth;

public partial class EmailVerificationPage : ContentPage
{
    public EmailVerificationPage(EmailVerificationViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
