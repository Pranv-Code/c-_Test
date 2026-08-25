using System.IO;
using System.Net.Http;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TestApp.Client.Services;
using TestApp.Client.ViewModels;

namespace TestApp.Client;

public partial class App : Application
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);

        ServiceProvider = serviceCollection.BuildServiceProvider();

        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        string baseUrl = configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7143/";
        if (!baseUrl.EndsWith("/")) baseUrl += "/";

        services.AddSingleton<IConfiguration>(configuration);

        services.AddHttpClient<ICommentApiService, CommentApiService>(client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        services.AddTransient<MainViewModel>();
        services.AddTransient<MainWindow>();
    }
}
