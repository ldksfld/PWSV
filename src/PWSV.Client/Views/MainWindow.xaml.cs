using System.Windows;
using PWSV.Client.Services.Interfaces;

namespace PWSV.Client.Views;

public partial class MainWindow : Window
{
    public MainWindow(INavigationService navigation)
    {
        InitializeComponent();
        DataContext = navigation;
    }
}
