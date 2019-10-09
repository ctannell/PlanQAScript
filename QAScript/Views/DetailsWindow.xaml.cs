using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Data;


namespace QAScript.Views
{
    /// <summary>
    /// Interaction logic for DetailsWindow.xaml
    /// </summary>
    public partial class DetailsWindow : Window
    {
        public DetailsWindow()
        {
            InitializeComponent();
            DataTable dt = SomeProperties.MsgDataTable;
            // Loop through datatable and add "unknown" to any test results that are not pass or fail
            foreach (DataRow row in dt.Rows)
            {
                if (row["Result"].ToString() == "")
                {
                    row["Result"] = "Unknown";
                }
            }

            ResultsTable.DataContext = dt;
            this.Topmost = true;
        }

        private void Close1_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
