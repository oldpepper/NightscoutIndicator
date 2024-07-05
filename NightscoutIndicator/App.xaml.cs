using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Configuration;
using System.IO;
using System.Windows;
using NightscoutIndicator.SettingsManagement;
using Application = System.Windows.Application;
//using Microsoft.Extensions.Options.ConfigurationExtensions;

namespace NightscoutIndicator
{
    /// <summary>
    /// Application.
    /// </summary>
    public partial class App : Application
    {
        public IServiceProvider ServiceProvider { get; private set; }
        
        protected override void OnStartup(StartupEventArgs e)
        {
            var services = new ServiceCollection();

            var basePath = AppContext.BaseDirectory;

            // On the first run the user settings are initialized with the defaults
            UserSettingsFile.EnsureCreated(Path.Combine(basePath, UserSettingsFile.DEFAULT_FILE_NAME));

            // Each next layer overrides the previous one: defaults -> user settings -> developer settings
            var builder = new ConfigurationBuilder()
                          .SetBasePath(basePath)
                          .AddJsonFile(UserSettingsFile.DEFAULT_FILE_NAME, optional: true, reloadOnChange: true)
                          .AddJsonFile(UserSettingsFile.FilePath, optional: true, reloadOnChange: true);
#if DEBUG
            builder.AddJsonFile(UserSettingsFile.DEVELOPMENT_FILE_NAME, optional: true, reloadOnChange: true);
#endif
            builder.SetFileLoadExceptionHandler(context =>
            {
                var contextException = context.Exception;
            });

            var configurationRoot = builder.Build();

            services.AddSingleton<IConfiguration>(configurationRoot);

            var commonSettingsSection = configurationRoot.GetSection(NightscoutIndicatorSettings.SECTION_NAME);
            var flyingIndicatorWindowSettingsSection = configurationRoot.GetSection(FlyingIndicatorWindowSettings.SECTION_NAME);

            services.Configure<NightscoutIndicatorSettings>(commonSettingsSection);
            services.Configure<FlyingIndicatorWindowSettings>(flyingIndicatorWindowSettingsSection);
            services.ConfigureWritable<FlyingIndicatorWindowSettings>(flyingIndicatorWindowSettingsSection, UserSettingsFile.FilePath);
            
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<FlyingIndicatorViewModel>();
            services.AddSingleton<DiagramViewModel>();
            services.AddSingleton<SettingsViewModel>();
            services.AddSingleton<NightscoutIndicatorSettings>();
            services.AddSingleton<FlyingIndicatorWindowSettings>();
            services.AddSingleton<DataReceiver>();
            services.AddSingleton<DiaCalculations>();
            services.AddSingleton<NightscoutClient>();
            services.AddSingleton(configurationRoot);

            ServiceProvider = services.BuildServiceProvider();

            var viewModel = ServiceProvider.GetRequiredService<MainViewModel>();

            base.OnStartup(e);
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Application.Exit" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.Windows.ExitEventArgs" /> that contains the event data.</param>
        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            var mainViewModel = ServiceProvider.GetService<MainViewModel>();
            mainViewModel.Dispose();
        }
    }
}
