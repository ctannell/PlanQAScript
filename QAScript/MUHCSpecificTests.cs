using System;
using System.IO;
using System.Linq;
using System.Windows;
using VMS.TPS.Common.Model.API;
using System.Data;

namespace QAScript
{
    class MUHCSpecificTests
    {
        public static void RunMUHC(Patient patient, Course course, PlanSetup plan)
        {
            // Every new class needs to do these same first steps which is to load in the results message and the datatable from their propertes and write them back at the end of the code.
            string msg = SomeProperties.MsgString;
            DataTable table = SomeProperties.MsgDataTable;
            //DataRow row;



            //////////////////////////////////////////////////////////////////////////////////////////////////////
            // Dan, please paste any code from the main code section that does not apply to you in this method. //
            //////////////////////////////////////////////////////////////////////////////////////////////////////




            // Write back current message and datatable
            SomeProperties.MsgString = msg;
            SomeProperties.MsgDataTable = table;
        }
    }
}
