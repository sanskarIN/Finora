namespace Finora.App;

public partial class OnboardingPage
{
    private async void OnOnboardingTermsClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync($"{nameof(LegalPage)}?document=terms");
}
