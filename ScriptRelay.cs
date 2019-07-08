using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Reflection;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace VMS.TPS
{
	public class Script
    {
		public Script()
        {
        }

        public void Execute(ScriptContext context)
        {
            var assemblyPath = @"Path to .exe goes here and put this file in your Eclipse script directory";
            var assem = Assembly.UnsafeLoadFrom(assemblyPath);
            var script = Activator.CreateInstanceFrom(assemblyPath, "VMS.TPS.XScript").Unwrap();
            var type = script.GetType();
            type.InvokeMember("Execute", BindingFlags.Default | BindingFlags.InvokeMethod, null, script, new object[] { context });
        }
    }
}