using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using VMS.TPS.Common.Model.API;

namespace QAScript
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        ////////////////////////////////////////////////////////////
        // This is where the code begins when run as a standalone //
        ////////////////////////////////////////////////////////////
        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            //// Manually define plan to be opened when run as a standalone app ////
            SomeProperties.PatientId = "QA_ESAPI"; // Set patient ID property
            SomeProperties.CourseId = "C1"; // Set course ID property
            SomeProperties.PlanId = "UnitTest0"; // Set plan ID property

            // Load general settigns;
            UserSettings.DefineUserSettings();

            // Create the application and connect to the API
            Patient patient = null;
            Course course = null;
            PlanSetup plan = null;
            try
            {
                // Create the application
                VMS.TPS.Common.Model.API.Application app = ESAPIApplication.Instance.Context;
                // Open the plan
                patient = app.OpenPatientById(SomeProperties.PatientId);
                course = patient.Courses.Where(c => c.Id == SomeProperties.CourseId).Single();
                plan = course.PlanSetups.Where(p => p.Id == SomeProperties.PlanId).Single();
            }
            catch
            {
                string errorlog = DateTime.Now + ", The script was not able to connect to the Eclipse API or load the plan info for some reason.";
                MessageBox.Show(errorlog);
                return;
            }

            // Launch the main part of the code.
            MainCode.RunMainCode(patient, course, plan);
        }

        ////////////////////////////////////////////////////////
        // This is where the code begins when run as a script //
        ////////////////////////////////////////////////////////
        public static void App_OnScriptStartup(ScriptContext context)
        {
            // Load general settigns;
            UserSettings.DefineUserSettings();

            // Make sure that there is a plan laoded and it's not a plan sum.
            if (context.PlanSetup != null)
            {
                Patient patient = context.Patient;
                Course course = context.Course;
                PlanSetup plan = context.PlanSetup;
                SomeProperties.PatientId = patient.Id;
                SomeProperties.CourseId = course.Id;
                SomeProperties.PlanId = plan.Id;
                SomeProperties.User = context.CurrentUser.Id;

                // Launch the main part of the code.
                MainCode.RunMainCode(patient, course, plan);
            }
            else
            {
                MessageBox.Show("A plan is not loaded. Please load a plan and try again.");
            }

        }
    }
}
