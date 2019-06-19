using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;


namespace VMS.TPS
{
    public class XScript : MarshalByRefObject // For more info on what this is see Rex Cardan's video at: https://www.youtube.com/watch?v=iVAQf_bsaZg&t
    {
        public void Execute(VMS.TPS.Common.Model.API.ScriptContext context)
        {
            QAScript.App.App_OnScriptStartup(context);
        }
    }
}
