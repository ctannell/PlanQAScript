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

namespace QAScript.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            QAMessage.Text = SomeProperties.MsgString;
            this.Topmost = true;
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            ESAPIApplication.Dispose();
            base.OnClosing(e);
        }

        private void Close1_Click(object sender, RoutedEventArgs e)
        {
            ESAPIApplication.Dispose();
            this.Close();
        }

        private void Details1_Click(object sender, RoutedEventArgs e)
        {
            DetailsWindow detailsWindow = new DetailsWindow();
            detailsWindow.Show();
        }
    }
}
