namespace AeroMech.UI.Web.Services
{
    public class ConfirmationService
    {
        public event Func<string, Task<bool>>? OnShow;

        public async Task<bool> ConfirmAsync(string message)
        {
            if (OnShow != null)
            {
                return await OnShow.Invoke(message);
            }
            return false;  
        }
    }
}
