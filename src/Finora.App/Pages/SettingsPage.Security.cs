namespace Finora.App;

public partial class SettingsPage
{
    private async void OnRemovePinSafelyClicked(object? sender, EventArgs e)
    {
        if (!await DisplayAlertAsync(
            "Remove app lock?",
            "Anyone with access to this device session may open Finora after the PIN is removed.",
            "Remove",
            "Cancel"))
            return;

        try
        {
            await _lock.ClearPinAsync();
            ViewModel.BiometricUnlock = false;
            await DisplayAlertAsync(
                "App lock removed",
                "The local PIN and biometric unlock preference have been removed.",
                "OK");
        }
        catch (Exception exception)
        {
            _logger.Error(exception, "Settings.PinRemovalFailed");
            await DisplayAlertAsync(
                "PIN not removed",
                "Finora could not remove the PIN verifier from device secure storage. The app lock remains enabled.",
                "OK");
        }
        finally
        {
            NewPinEntry.Text = string.Empty;
            ConfirmPinEntry.Text = string.Empty;
        }
    }
}
