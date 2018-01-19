using System;

using GalaSoft.MvvmLight.Ioc;

using IoTPlayer.Services;
using IoTPlayer.Views;

using Microsoft.Practices.ServiceLocation;

namespace IoTPlayer.ViewModels
{
    public class ViewModelLocator
    {
        public ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

            SimpleIoc.Default.Register(() => new NavigationServiceEx());
            SimpleIoc.Default.Register<ShellViewModel>();
            Register<MediaPlayerViewModel, MediaPlayerPage>();
        }

        public MediaPlayerViewModel MediaPlayerViewModel => ServiceLocator.Current.GetInstance<MediaPlayerViewModel>();

        public ShellViewModel ShellViewModel => ServiceLocator.Current.GetInstance<ShellViewModel>();

        public NavigationServiceEx NavigationService => ServiceLocator.Current.GetInstance<NavigationServiceEx>();

        public void Register<VM, V>()
            where VM : class
        {
            SimpleIoc.Default.Register<VM>();

            NavigationService.Configure(typeof(VM).FullName, typeof(V));
        }
    }
}
