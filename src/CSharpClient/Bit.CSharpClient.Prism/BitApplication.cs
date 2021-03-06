﻿using Autofac;
using Autofac.Extensions.DependencyInjection;
using Bit.Model.Events;
using Bit.View;
using Bit.View.Contracts;
using Bit.ViewModel;
using Bit.ViewModel.Contracts;
using Bit.ViewModel.Implementations;
using Microsoft.Extensions.DependencyInjection;
using Prism;
using Prism.Autofac;
using Prism.Events;
using Prism.Ioc;
using Prism.Logging;
using Prism.Navigation;
using Prism.Plugin.Popups;
using Rg.Plugins.Popup.Services;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]

#if !UWP
[assembly: XmlnsDefinition("https://bit-framework.com", "Bit", AssemblyName = "Bit.CSharpClient.Prism")]
[assembly: XmlnsDefinition("https://bit-framework.com", "Bit.View", AssemblyName = "Bit.CSharpClient.Prism")]
[assembly: XmlnsDefinition("https://bit-framework.com", "Bit.View.Props", AssemblyName = "Bit.CSharpClient.Prism")]
#endif

namespace Bit
{
    public abstract class BitApplication : PrismApplication, IAdaptiveBehaviorService
    {
        /// <summary>
        /// To be called in shared/net-standard project.
        /// https://docs.microsoft.com/bg-bg/xamarin/xamarin-forms/xaml/custom-namespace-schemas#consuming-a-custom-namespace-schema
        /// </summary>
        public static void XamlInit()
        {

        }

        public BitApplication()
            : this(null)
        {
            
        }

        protected BitApplication(IPlatformInitializer platformInitializer = null)
            : base(platformInitializer)
        {
            _eventAggregator = new Lazy<IEventAggregator>(() =>
            {
                return Container.Resolve<IEventAggregator>();
            }, isThreadSafe: true);

            if (MainPage == null)
                MainPage = new ContentPage { Title = "DefaultPage" };
        }

        protected override void OnPropertyChanging([CallerMemberName] string propertyName = null)
        {
            base.OnPropertyChanging(propertyName);

            if (propertyName == nameof(MainPage) && MainPage != null)
            {
                MainPage.SizeChanged -= MainPage_SizeChanged;
            }
        }

        protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);

            if (propertyName == nameof(MainPage) && MainPage != null)
            {
                MainPage.SizeChanged += MainPage_SizeChanged;
            }
        }

        private readonly Lazy<IEventAggregator> _eventAggregator;

        private void MainPage_SizeChanged(object sender, EventArgs e)
        {
            PublishOrientationAndOrSizeChangedEvent();
        }

        protected virtual void PublishOrientationAndOrSizeChangedEvent()
        {
            double width = MainPage.Width;
            double height = MainPage.Height;

            if (width == 0 || height == 0)
                return;

            DeviceOrientation newOrientation = (width < height) ? DeviceOrientation.Portrait : DeviceOrientation.Landscape;

            _eventAggregator?.Value.GetEvent<OrientationAndOrSizeChanged>().Publish(new OrientationAndOrSizeChanged
            {
                NewOrientation = newOrientation,
                NewWidth = width,
                NewHeight = height
            });
        }

        protected sealed override async void OnInitialized()
        {
            try
            {
                await OnInitializedAsync();
                await Task.Yield();
            }
            catch (Exception exp)
            {
                BitExceptionHandler.Current.OnExceptionReceived(exp);
            }
        }

        protected virtual Task OnInitializedAsync()
        {
            Connectivity.ConnectivityChanged += (sender, e) =>
            {
                _eventAggregator.Value.GetEvent<ConnectivityChangedEvent>()
                    .Publish(new ConnectivityChangedEvent { IsConnected = e.NetworkAccess != NetworkAccess.None });
            };

            return Task.CompletedTask;
        }

        public new INavService NavigationService => PrismNavigationService == null ? null : new DefaultNavService
        {
            PrismNavigationService = PrismNavigationService,
            PopupNavigation = PopupNavigation.Instance
        };

        public static new BitApplication Current => (PrismApplicationBase.Current as BitApplication);

        public INavigationService PrismNavigationService => base.NavigationService;

        protected sealed override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            ContainerBuilder containerBuilder = containerRegistry.GetBuilder();

            IServiceCollection services = new BitServiceCollection();

            containerBuilder.Properties[nameof(services)] = services;
            containerBuilder.Properties[nameof(containerBuilder)] = containerBuilder;

            RegisterTypes(containerRegistry, containerBuilder, services);

            containerBuilder.Populate(services);
        }

        protected virtual void RegisterTypes(IContainerRegistry containerRegistry, ContainerBuilder containerBuilder, IServiceCollection services)
        {
            containerRegistry.Register<ILoggerFacade, BitPrismLogger>();
            containerBuilder.Register(c => Container).SingleInstance().PreserveExistingDefaults();
            containerBuilder.Register(c => Container.GetContainer()).PreserveExistingDefaults();
            containerBuilder.Register<IAdaptiveBehaviorService>(c => this).SingleInstance().PreserveExistingDefaults();
            PopupNavigation.SetInstance(new BitPopupNavigation
            {
                OriginalImplementation = PopupNavigation.Instance
            });
            containerRegistry.RegisterPopupNavigationService();
        }

        protected override IContainerExtension CreateContainerExtension()
        {
            return new AutofacContainerExtension(new ContainerBuilder());
        }

        public virtual void InvalidateAllAdptiveBehaviors()
        {
            _eventAggregator.Value.GetEvent<InvalidateAllAdaptiveBehaviors>()
                .Publish(new InvalidateAllAdaptiveBehaviors { });

            PublishOrientationAndOrSizeChangedEvent();
        }
    }

    public class BitServiceCollection : List<ServiceDescriptor>, IServiceCollection
    {

    }
}
