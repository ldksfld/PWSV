using System.Globalization;
using System.IO;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PWSV.Client.Services;
using PWSV.Client.Services.Interfaces;
using PWSV.Client.ViewModels;
using PWSV.Client.ViewModels.Accounts;
using PWSV.Client.ViewModels.Categories;
using PWSV.Client.ViewModels.Currencies;
using PWSV.Client.ViewModels.Transactions;
using PWSV.Client.Views;
using Serilog;

namespace PWSV.Client;

public partial class App : Application
{
    private IHost? _host;

    public App()
    {
        var culture = CultureInfo.GetCultureInfo("uk-UA");
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(culture.IetfLanguageTag)));
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = Host.CreateDefaultBuilder()
            .UseContentRoot(AppContext.BaseDirectory)
            .ConfigureAppConfiguration((_, config) =>
            {
                config.SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false);
            })
            .UseSerilog((context, services, logger) =>
            {
                var path = Environment.ExpandEnvironmentVariables(
                    context.Configuration["Logging:Path"] ?? "%LOCALAPPDATA%/PWSV/logs/client-.log");
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                logger
                    .MinimumLevel.Information()
                    .WriteTo.File(path,
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 14);
            })
            .ConfigureServices(ConfigureServices)
            .Build();

        DispatcherUnhandledException += (_, args) =>
        {
            Log.Error(args.Exception, "Unhandled UI exception");
            var details = args.Exception.Message;
            MessageBox.Show(
                $"Виникла неочікувана помилка:\n\n{details}\n\nДеталі — у файлі логів.",
                "Помилка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                Log.Error(ex, "Unhandled domain exception");
            }
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Log.Error(args.Exception, "Unobserved task exception (background fetch)");
            args.SetObserved();
        };

        var navigation = _host.Services.GetRequiredService<INavigationService>();
        var tokenStorage = _host.Services.GetRequiredService<ITokenStorage>();
        tokenStorage.AuthenticationChanged += () =>
        {
            if (tokenStorage.IsAuthenticated || navigation.CurrentViewModel is LoginViewModel or RegisterViewModel)
            {
                return;
            }

            Dispatcher.Invoke(() => navigation.NavigateTo<LoginViewModel>());
        };
        navigation.NavigateTo<LoginViewModel>();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        var baseAddress = context.Configuration["Api:BaseAddress"] ?? "https://localhost:5001";
        var timeoutSeconds = int.TryParse(context.Configuration["Api:TimeoutSeconds"], out var t) ? t : 30;

        services.AddSingleton<ITokenStorage, TokenStorage>();
        services.AddTransient<AuthTokenDelegatingHandler>();

        services.AddHttpClient(ApiClient.HttpClientName, client =>
        {
            client.BaseAddress = new Uri(baseAddress);
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }).AddHttpMessageHandler<AuthTokenDelegatingHandler>();

        services.AddSingleton<IApiClient, ApiClient>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IDialogService, DialogService>();

        services.AddTransient<LoginViewModel>();
        services.AddTransient<RegisterViewModel>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<AccountsListViewModel>();
        services.AddSingleton<CategoriesViewModel>();
        services.AddSingleton<CurrenciesViewModel>();
        services.AddSingleton<ViewModels.ExchangeRates.ExchangeRatesViewModel>();
        services.AddSingleton<TransactionsListViewModel>();
        services.AddTransient<ViewModels.Accounts.AccountDetailsViewModel>();
        services.AddTransient<ViewModels.Accounts.AccountEditViewModel>();
        services.AddTransient<ViewModels.Categories.CategoryEditViewModel>();
        services.AddTransient<ViewModels.Transactions.TransactionEditViewModel>();

        services.AddSingleton<MainWindow>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.CloseAndFlush();
        _host?.Dispose();
        base.OnExit(e);
    }
}
