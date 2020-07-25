﻿using System;
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
            Application.Init();
            var topLevel = Application.Top;

            var mainView = new MainView(topLevel);
            mainView.Create();

            _isUIShowing = true;

            // Demonstrate how a background task can push updates into a view.
            Task.Run(async () =>
            {
                while (_isUIShowing)
                {
                    await Task.Delay(1000);
                   
                    mainView.UpdateClockInStatusBar();
                }
            });

            // this will block
            Application.Run(topLevel);

            _isUIShowing = false;
        }
    }
}
