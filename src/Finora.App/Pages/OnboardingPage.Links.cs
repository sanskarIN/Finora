namespace Finora.App;

public partial class OnboardingPage
{
    private async void OnOnboardingTermsClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync($"{nameof(LegalPage)}?document=terms");

    private async void OnOnboardingBuyMeACoffeeClicked(object? sender, EventArgs e)
    {
        const string title = "Support Finora";
        try
        {
            var opened = await Launcher.Default.OpenAsync(new Uri(Finora.Shared.AppConstants.BuyMeACoffeeUrl));
            if (!opened)
                await DisplayAlertAsync(title, "The Buy Me a Coffee support page could not be opened with the available browser or system handler.", "OK");
        }
        catch (Exception)
        {
            await DisplayAlertAsync(title, "The Buy Me a Coffee support page could not be opened right now.", "OK");
        }
    }
}
