﻿using Bit.Model;
using Bit.ViewModel.Contracts;
using Prism.Navigation;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.ViewModel
{
    public class BitViewModelBase : Bindable, INavigatedAware, IInitializeAsync, INavigationAware, IDestructible
    {
        public virtual CancellationTokenSource CancellationTokenSource { get; set; }

        public virtual CancellationToken CurrentCancellationToken { get; set; }

        public BitViewModelBase()
        {
            CancellationTokenSource = new CancellationTokenSource();
            CurrentCancellationToken = CancellationTokenSource.Token;
        }

        public async void Destroy()
        {
            try
            {
                try
                {
                    CancellationTokenSource.Cancel();
                }
                finally // make sure that OnDestroyAsync gets called.
                {
                    await OnDestroyAsync();
                    await Task.Yield();
                }
            }
            catch (Exception exp)
            {
                BitExceptionHandler.Current.OnExceptionReceived(exp);
            }
        }

        public virtual Task OnDestroyAsync()
        {
            return Task.CompletedTask;
        }

        public async void OnNavigatedFrom(INavigationParameters parameters)
        {
            try
            {
                await OnNavigatedFromAsync(parameters);
                await Task.Yield();
            }
            catch (Exception exp)
            {
                BitExceptionHandler.Current.OnExceptionReceived(exp);
            }
        }

        public virtual Task OnNavigatedFromAsync(INavigationParameters parameters)
        {
            return Task.CompletedTask;
        }

        protected virtual string GetViewModelName()
        {
            return GetType().Name.Replace("ViewModel", string.Empty);
        }

        protected virtual bool ShouldLogNavParam(string navParamName)
        {
            return false;
        }

        public async void OnNavigatedTo(INavigationParameters parameters)
        {
            DateTimeOffset startDate = DateTimeOffset.Now;
            bool success = true;

            try
            {
                await Task.Yield();
                await OnNavigatedToAsync(parameters);
                await Task.Yield();
            }
            catch (Exception exp)
            {
                success = false;
                BitExceptionHandler.Current.OnExceptionReceived(exp);
            }
            finally
            {
                if (parameters.GetNavigationMode() == NavigationMode.New)
                {
                    string pageName = GetViewModelName();

                    Dictionary<string, string> properties = new Dictionary<string, string> { };

                    foreach (KeyValuePair<string, object> prp in parameters)
                    {
                        if (ShouldLogNavParam(prp.Key))
                            properties.Add(prp.Key, prp.Value?.ToString() ?? "NULL");
                    }

                    properties.Add("PageViewSucceeded", success.ToString(CultureInfo.InvariantCulture));

                    TimeSpan duration = DateTimeOffset.Now - startDate;

                    TelemetryServices.All().TrackPageView(pageName, duration, properties);
                }
            }
        }

        public virtual Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            return Task.CompletedTask;
        }

        public async Task InitializeAsync(INavigationParameters parameters)
        {
            try
            {
                await Task.Yield();
                await OnInitializeAsync(parameters);
            }
            catch (Exception exp)
            {
                BitExceptionHandler.Current.OnExceptionReceived(exp);
            }
        }

        public virtual Task OnInitializeAsync(INavigationParameters parameters)
        {
            return Task.CompletedTask;
        }

        public virtual INavService NavigationService { get; set; }

        public virtual IEnumerable<ITelemetryService> TelemetryServices { get; set; }
    }
}
