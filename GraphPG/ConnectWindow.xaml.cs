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

namespace GraphPG
{
    /// <summary>
    /// Window2.xaml 的交互逻辑
    /// </summary>
    public partial class ConnectWindow : Window
    {
        public ConnectWindow()
        {
            InitializeComponent();
        }

        private void ButtonForConnect_Click(object sender, RoutedEventArgs e)
        {
            var connectString = String.Format("Host={0};Username={1};Password={2};Database={3};",
                                               TextBoxForHost.Text, TextBoxForUsername.Text, Password.Password, TextBoxForDBName.Text);
            string resString;
            if (!Controller.GetController().ConnectDB(connectString, out resString))
            {
                LabelForErrorHint.Content = resString;
                LabelForErrorHint.Visibility = Visibility.Visible;
            }
            else
                this.Close();
        }
    }
}

