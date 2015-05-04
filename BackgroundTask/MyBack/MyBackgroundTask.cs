using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;

namespace MyBack
{
    public sealed class MyBackgroundTask : IBackgroundTask
    {
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            var cost = BackgroundWorkCost.CurrentBackgroundWorkCost;
            if (cost == BackgroundWorkCostValue.High)
            {
                return;
            }

            var cancel = false;
            taskInstance.Canceled += (s, e) =>
            {
                cancel = true;
            };

            var deferral = taskInstance.GetDeferral();
            try
            {
                // retrieve arguments
                var details = taskInstance.TriggerDetails as ApplicationTriggerDetails;
                var args = details.Arguments as ValueSet;
                var arg = args["Argument"].ToString();
                var value = int.Parse(arg);

                // run operation
                await Task.Run(async () =>
                {
                    for (int i = 0; i < value; i++)
                    {
                        if (cancel)
                            return;
                        taskInstance.Progress = (uint)i;
                        await Task.Delay(200);
                    }
                });
            }
            catch { /* TODO */ }
            finally { deferral.Complete(); }
        }
    }
}
