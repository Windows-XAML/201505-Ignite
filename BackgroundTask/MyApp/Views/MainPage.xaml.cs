﻿using MyBack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MyApp.Views
{
    public sealed partial class MainPage : Page
    {
        private ApplicationTrigger _applicationTrigger;

        public MainPage()
        {
            this.InitializeComponent();
            Loaded += MainPage_Loaded;
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateButtons();
        }

        private void registerButton_Click(object sender, RoutedEventArgs args)
        {
            Register();
            UpdateButtons();
        }

        private void unregisterButton_Click(object sender, RoutedEventArgs e)
        {
            Unregister();
            UpdateButtons();
        }

        private async void invokeButton_Click(object sender, RoutedEventArgs e)
        {
            registerButton.IsEnabled = false;
            unregisterButton.IsEnabled = false;
            invokeButton.IsEnabled = false;
            await Invoke();
        }

        private async Task Register()
        {
            if (ExistingRegistration.Any())
                return;

            // request access
            var access = BackgroundExecutionManager.GetAccessStatus();
            switch (access)
            {
                case BackgroundAccessStatus.Unspecified:
                case BackgroundAccessStatus.Denied:
                    {
                        await new ContentDialog { Title = "Cannot register", Content = "Get Access: " + access.ToString(), PrimaryButtonText = "Close" }.ShowAsync();
                        return;
                    }
            }

            // build task
            var task = new BackgroundTaskBuilder
            {
                Name = nameof(MyBackgroundTask),
                CancelOnConditionLoss = true,
                TaskEntryPoint = typeof(MyBackgroundTask).ToString(),
            };
            task.SetTrigger(this._applicationTrigger = new ApplicationTrigger());

            // register
            BackgroundTaskRegistration registration = null;
            try { registration = task.Register(); }
            catch (Exception ex)
            {
                this.ProgressText.Text = "Registration failed: " + ex.Message;
                return;
            }

            // report
            var dispatcher = this.Dispatcher;
            registration.Progress += async (s, e) =>
            {
                await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
                {
                    this.ProgressText.Text = string.Format("Task progress: {0}", e.Progress);
                });
            };

            // handle complete
            registration.Completed += async (s, e) =>
            {
                await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
                {
                    try
                    {
                        e.CheckResult();
                        this.ProgressText.Text = "Task completed.";
                    }
                    catch (Exception ex)
                    {
                        this.ProgressText.Text = string.Format("Task error {0}", ex.Message);
                    }
                    finally { UpdateButtons(); }
                });
            };
        }

        private void Unregister()
        {
            var existingRegistration = ExistingRegistration;
            if (existingRegistration.Any())
            {
                foreach (var registration in existingRegistration)
                    registration.Unregister(stopCheckBox.IsChecked.Value);
            }
        }

        private async Task Invoke()
        {
            // setup arguments
            int value;
            if (!int.TryParse(this.targetTextBox.Text, out value))
            {
                await new ContentDialog { Content = "Invalid target integer", PrimaryButtonText = "Close" }.ShowAsync();
                return;
            }
            var args = new ValueSet();
            args["Argument"] = value;

            // invoke
            var result = await this._applicationTrigger.RequestAsync(args);

            // did it work?
            switch (result)
            {
                case ApplicationTriggerResult.Allowed:
                    break;
                case ApplicationTriggerResult.CurrentlyRunning:
                case ApplicationTriggerResult.DisabledByPolicy:
                case ApplicationTriggerResult.UnknownError:
                    await new ContentDialog { Content = result.ToString(), PrimaryButtonText = "Close" }.ShowAsync();
                    break;
            }
            this.ProgressText.Text = "Running";
        }

        public IEnumerable<IBackgroundTaskRegistration> ExistingRegistration
        {
            get
            {
                var result = BackgroundTaskRegistration.AllTasks
                    .Where(x => x.Value.Name.Equals(nameof(MyBackgroundTask)))
                    .Select(x => x.Value);
                return result;
            }
        }

        void UpdateButtons()
        {
            registerButton.IsEnabled = !ExistingRegistration.Any();
            unregisterButton.IsEnabled = !registerButton.IsEnabled;
            invokeButton.IsEnabled = !registerButton.IsEnabled;
        }
    }
}
