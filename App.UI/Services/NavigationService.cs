using Avalonia.Controls;
using App.Core;
using App.UI.ViewModels;
using App.UI.Views;
using App.Factory;

namespace App.UI.Services
{
    public class NavigationService
    {
        private readonly string _connectionString;

        public NavigationService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void NavigateToDashboard(LoginResult loginResult, bool useEF, Window? loginWindow = null)
        {
            // Only Citizens have dashboard implemented for now
            if (loginResult.RoleID == 2 && !string.IsNullOrEmpty(loginResult.CitizenID))
            {
                var service = ServiceFactory.CreateCitizenService(useEF, _connectionString);
                var viewModel = new CitizenDashboardViewModel(service, loginResult.CitizenID, _connectionString, useEF);
                var dashboard = new CitizenDashboardWindow { DataContext = viewModel };

                dashboard.Show();
                loginWindow?.Close();
            }
            else
            {
                // For other roles, show message for now
                // TODO: Implement Operator and Government dashboards
                loginWindow?.Close();
            }
        }
    }
}