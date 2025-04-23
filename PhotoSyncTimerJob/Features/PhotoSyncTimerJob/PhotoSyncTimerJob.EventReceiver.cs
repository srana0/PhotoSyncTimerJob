using System;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;

namespace CC_PhotoSyncTimerJob.Features.PhotoSyncTimerJob
{
    /// <summary>
    /// This class handles events raised during feature activation, deactivation, installation, uninstallation, and upgrade.
    /// </summary>
    /// <remarks>
    /// The GUID attached to this class may be used during packaging and should not be modified.
    /// </remarks>

    [Guid("d52f920e-61b9-4d24-8ed7-94ea59fa1e5c")]
    public class PhotoSyncTimerJobEventReceiver : SPFeatureReceiver
    {
        const string JobName = "Colleague Connect Photo Synchronization";
        // Uncomment the method below to handle the event raised after a feature has been activated.

        public override void FeatureActivated(SPFeatureReceiverProperties properties)
        {
            SPWebApplication webapplication = properties.Feature.Parent as SPWebApplication;
            DeleteJob(webapplication);
            CreateJob(webapplication); 
        }


        // Uncomment the method below to handle the event raised before a feature is deactivated.

        public override void FeatureDeactivating(SPFeatureReceiverProperties properties)
        {
            SPWebApplication webapplication = properties.Feature.Parent as SPWebApplication;
            DeleteJob(webapplication);
        }

        //Delete Job
        private static void DeleteJob(SPWebApplication webapplication)
        {
            try
            {
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {

                    foreach (SPJobDefinition job in webapplication.JobDefinitions)
                    {
                        if (job.Name == JobName)
                            job.Delete();
                    }

                });

            }          

            catch (Exception ex)
            {
                Microsoft.Office.Server.Diagnostics.PortalLog.LogString("PhotoSyncTimerJobEventReceiver-DeleteJob, Exception: {0} ::: {1}", ex.Message, ex.StackTrace);
            }

        }//end of DeleteJob



        //Start of Create Job
        private static void CreateJob(SPWebApplication webapplication)
        {

            try
            {
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {

                    ProfilePictureSync timer = new ProfilePictureSync(JobName, webapplication);                    
                    SPWeeklySchedule schedule = new SPWeeklySchedule();
                    schedule.BeginHour = 00;
                    schedule.BeginMinute = 00;
                    schedule.BeginSecond = 00;

                    schedule.EndHour = 01;
                    schedule.EndMinute = 01;
                    schedule.EndSecond = 00;

                    timer.Schedule = schedule;
                    timer.Update();

                });

            }

            catch (Exception ex)
            {
                Microsoft.Office.Server.Diagnostics.PortalLog.LogString("PhotoSyncTimerJobEventReceiver-CreateJob, Exception: {0} ::: {1}", ex.Message, ex.StackTrace);
            }                

        }//end of create Job

        // Uncomment the method below to handle the event raised after a feature has been installed.

        //public override void FeatureInstalled(SPFeatureReceiverProperties properties)
        //{
        //}


        // Uncomment the method below to handle the event raised before a feature is uninstalled.

        //public override void FeatureUninstalling(SPFeatureReceiverProperties properties)
        //{
        //}

        // Uncomment the method below to handle the event raised when a feature is upgrading.

        //public override void FeatureUpgrading(SPFeatureReceiverProperties properties, string upgradeActionName, System.Collections.Generic.IDictionary<string, string> parameters)
        //{
        //}
    }
}
