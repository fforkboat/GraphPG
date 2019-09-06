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
    /// ConfirmationDialog.xaml 的交互逻辑
    /// </summary>
    public partial class ConfirmationDialog : Window
    {
        public ConfirmationDialog(Window ownerWindow, string message)
        {
            InitializeComponent();
            Owner = ownerWindow;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            LabelForMessage.Content = message;
        }

        public bool IsConfirm = false;

        private void ButtonForConfirm_Click(object sender, RoutedEventArgs e)
        {
            IsConfirm = true;

            Close();
        }

        private void ButtonForCancel_Click(object sender, RoutedEventArgs e)
        {
            IsConfirm = false;

            Close();
        }
    }
}
