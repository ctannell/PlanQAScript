using System;
using System.Windows;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMS.TPS.Common.Model.API;
using QAScript.Views;

namespace QAScript
{
    public class MainCode
    {
        public static void RunMainCode(Patient patient, Course course, PlanSetup plan)
        {
            // Launch the class that uses a direct DB query to gather info that API does not have access to. Inside this class the code will be the same between the standalone and script versions.
            DBQuery.RunDBQuery(patient, course, plan);

            // Launch the class that does general plan checks from API retrieved data. Inside this class the code will be the same between the standalone and script versions.
            GeneralTests.RunGeneralTests(patient, course, plan);

            // Same as above, but it includes the code that's specific to the MUHC
            MUHCSpecificTests.RunMUHC(patient, course, plan);

            //// Run any other hospital-specific or unique tests here in a new class as done above. ////
            //                                                                                        //
            ////                                                                                    ////


            // Check message to see if anything was found and update the msg property.
            string msg = SomeProperties.MsgString;
            if (msg == "")
            {
                SomeProperties.MsgString = "No issues found!";
            }
            else
            {
                SomeProperties.MsgString = "Some issues found!" + SomeProperties.MsgString;
            }

            //Log useage and results to mapped, shared drive
            if (SomeProperties.LoggingFlag == true)
            {
                msg = SomeProperties.MsgString.Replace("\n\n", " - ");
                var user = SomeProperties.User;
                string log = DateTime.Now + " - " + user + " - " + patient.Id + " - " + course.Id + " - " + plan.Id + " - " + SomeProperties.ScriptVersion + " - " + msg;
                string path = SomeProperties.LogPath;

                using (StreamWriter sw = File.AppendText(path))
                {
                    sw.WriteLine(log);
                }
            }

            // Display the main window with the results
            MainWindow mw = new MainWindow();
            mw.Show();
        }
    }
}
