using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace QAScript
{
    public class SomeProperties
    {
        // Basically this acts as a global way to store things that all classes have access to.
        public static Boolean LoggingFlag { get; set; }
        public static string MsgString { get; set; }
        public static DataTable MsgDataTable { get; set; }
        public static string PatientId { get; set; }
        public static string CourseId { get; set; }
        public static string PlanId { get; set; }
        public static string DBDataSource { get; set; }
        public static string DBUserId { get; set; }
        public static string DBPassword { get; set; }
        public static string LogPath { get; set; }
        public static string User { get; set; }
        public static string ScriptVersion { get; set; }
    }
}
