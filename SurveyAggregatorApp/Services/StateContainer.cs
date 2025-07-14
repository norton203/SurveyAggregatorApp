// Services/StateContainer.cs
namespace SurveyAggregatorApp.Services
{
    public class StateContainer
    {
        public Models.User? CurrentUser { get; private set; }

        public event Action? OnChange;

        public void SetUser(Models.User user)
        {
            CurrentUser = user;
            NotifyStateChanged();
        }

        public void ClearUser()
        {
            CurrentUser = null;
            NotifyStateChanged();
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
