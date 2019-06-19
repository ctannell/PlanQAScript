using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace QAScript
{
    public class UserSettings
    {
        public static void DefineUserSettings()
        {
            SomeProperties.User = "ESAPI Standalone"; // Aria username (just for logging purposes in standalone mode)
            SomeProperties.ScriptVersion = "v2.0"; //Used to track what version was used in the useage log
            SomeProperties.LoggingFlag = true; // Do you want to write to the log file?
            SomeProperties.LogPath = @"I:\Tanner\ESAPI\QAScript\log.txt"; // Path for log file.
            SomeProperties.DBDataSource = "172.xxx.xxx.xxx"; // IP of varian DB.
            SomeProperties.DBUserId = "username"; // Username for Varian DB.
            SomeProperties.DBPassword = "password"; // Password for Varian DB.
            SomeProperties.MsgString = ""; //Initialize the message string

            // Create a new DataTable to store QA test results
            System.Data.DataTable table = new DataTable("DetailsTable");
            DataColumn column;

            column = new DataColumn();
            column.DataType = System.Type.GetType("System.String");
            column.ColumnName = "Item";
            table.Columns.Add(column);

            column = new DataColumn();
            column.DataType = System.Type.GetType("System.String");
            column.ColumnName = "Result";
            table.Columns.Add(column);

            SomeProperties.MsgDataTable = table; //We've now created the data table that holds the tests and the result. Only used in the "details" window of the GUI.
        }
    }
}
