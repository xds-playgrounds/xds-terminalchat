using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Terminal.Gui;
using XDS.Messaging.SDK.ApplicationBehavior.Services.PortableImplementations;
using XDS.Messaging.TerminalChat.ChatUI;

namespace XDS.Messaging.TerminalChat
{
    static class App
    {
        public static IServiceProvider ServiceProvider;

        public static bool IsOnboardingRequired { get; set; }

        static bool _isUIShowing;

        /// <summary>
        /// Here we use the Microsoft service collection. IServiceCollection is available as package cross-platform in the
        /// Microsoft.Extensions.DependencyInjection.Abstractions namespace. However, the ServiceCollection class is not cross-platform.
        /// For platforms like Blazor, or Xamarin, you need to implement your own version of
        /// IServiceCollection.
        /// </summary>
        /// <returns>An empty IServiceCollection for the application.</returns>
        public static IServiceCollection CreateServiceCollection()
        {
            return new ServiceCollection();
        }

        public static IServiceProvider CreateServiceProvider(IServiceCollection serviceCollection)
        {
            IDependencyInjection dependencyInjection = new PortableDependencyInjection(serviceCollection);

            serviceCollection.AddSingleton(dependencyInjection);

            ServiceProvider = serviceCollection.BuildServiceProvider();

            dependencyInjection.AddServiceProvider(ServiceProvider);
            return ServiceProvider;
        }

        public static void ShowUI()
        {
            Application.UseSystemConsole = true;
            Application.Init();
            
            var topLevel = Application.Top;
            //topLevel.ColorScheme.Focus = new Terminal.Gui.Attribute(Color.Black,Color.Black);
            //topLevel.ColorScheme.HotFocus = new Terminal.Gui.Attribute(Color.Black, Color.Black);
            //topLevel.ColorScheme.Normal = new Terminal.Gui.Attribute(Color.Black, Color.Black);
            //topLevel.ColorScheme.HotNormal = new Terminal.Gui.Attribute(Color.Black, Color.Black);
            //topLevel.ColorScheme.Disabled = new Terminal.Gui.Attribute(Color.Black, Color.Black);

            var mainView = new MainView(topLevel);
            mainView.Create();

            _isUIShowing = true;

            // Demonstrate how a background task can push updates into a view.
            Task.Run(async () =>
            {
                while (_isUIShowing)
                {
                    await Task.Delay(1000);
                   
                    NavigationService.UpdateClockInStatusBar();
                }
            });

            // this will block
            Application.Run(topLevel);
            Application.Shutdown();

            if (IsOnboardingRequired)
            {
                Environment.Exit(0);
            }

            _isUIShowing = false;
        }
    }
}
