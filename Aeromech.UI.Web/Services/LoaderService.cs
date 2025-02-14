namespace AeroMech.UI.Web.Services
{
    public class LoaderService
    {
        public event Action<bool> OnLoadingChanged;

        public void ShowLoader()
        {
            OnLoadingChanged?.Invoke(true);
        }

        public void HideLoader()
        {
            OnLoadingChanged?.Invoke(false);
        }
    }
}
