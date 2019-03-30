using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls;

namespace GraphPG
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            Controller.GetController().SetMainWindow(this);
        }

        public void Tree_DB_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem treeItem = (TreeViewItem)sender;
            if (treeItem == null || e.Handled)
                return;
            treeItem.IsExpanded = !treeItem.IsExpanded;

            e.Handled = true;
        }

        public void Tree_Table_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var table = (TreeViewItem)sender;
            var database = (TreeViewItem)table.Parent;

            Controller.GetController().OpenTable(database.Header.ToString(), table.Header.ToString());
            e.Handled = true;
        }

        private void Button_ConnectDB_Click(object sender, RoutedEventArgs e)
        {
            var connectForm = new ConnectWindow();
            connectForm.Owner = this;
            connectForm.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            connectForm.ShowDialog();
        }

        private void ButtonForDisConnect_Click(object sender, RoutedEventArgs e)
        {
        }

        private void DataGridForTable_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            string a = (DataGridForTable.ItemsSource as DataView).Table.Rows[e.Row.GetIndex()][e.Column.DisplayIndex].ToString();
            if ((e.EditingElement as TextBox).Text.Equals(a))
                return;

            GridForModifyTableAction.Visibility = Visibility.Visible;
            DBTreeView.IsEnabled = false;
            ToolBarMain.IsEnabled = false;
            GridForAddAndRemoveRow.IsEnabled = false;
        }

        private void ButtonForSaveChanges_Click(object sender, RoutedEventArgs e)
        {
            Controller.GetController().UpdateTable();

            GridForModifyTableAction.Visibility = Visibility.Collapsed;
            DBTreeView.IsEnabled = true;
            ToolBarMain.IsEnabled = true;
            GridForAddAndRemoveRow.IsEnabled = true;
           
        }

        private void ButtonForCancelChanges_Click(object sender, RoutedEventArgs e)
        {
            Controller.GetController().RestoreTable();

            GridForModifyTableAction.Visibility = Visibility.Collapsed;
            DBTreeView.IsEnabled = true;
            ToolBarMain.IsEnabled = true;
            GridForAddAndRemoveRow.IsEnabled = true;
            DataGridForTable.CellEditEnding += DataGridForTable_CellEditEnding;
        }

        private void StatusLabel_MouseLeave(object sender, MouseEventArgs e)
        {
            LabelForStatus.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
        }

        private void ButtonForAddRow_Click(object sender, RoutedEventArgs e)
        {
            var dataView = DataGridForTable.ItemsSource as DataView;
            dataView.AddNew();
            DataGridForTable.CellEditEnding -= DataGridForTable_CellEditEnding;
            GridForModifyTableAction.Visibility = Visibility.Visible;
            DBTreeView.IsEnabled = false;
            ToolBarMain.IsEnabled = false;
            GridForAddAndRemoveRow.IsEnabled = false;

            for (int i = 0; i < dataView.Count - 1 ; i++)
            {
                var row = DataGridForTable.ItemContainerGenerator.ContainerFromIndex(i) as DataGridRow;
                row.IsEnabled = false;
            }
            var newRow = DataGridForTable.ItemContainerGenerator.ContainerFromIndex(4) as DataGridRow;
            if (newRow == null)
            {
                DataGridForTable.UpdateLayout();
                DataGridForTable.ScrollIntoView(DataGridForTable.Items[4]);
                newRow = DataGridForTable.ItemContainerGenerator.ContainerFromIndex(4) as DataGridRow;
            }
            newRow.IsSelected = true;
        }

        private void ButtonForRemoveRow_Click(object sender, RoutedEventArgs e)
        {
            Controller.GetController().RemoveRow(DataGridForTable.SelectedIndex);

        }

        private void DataGridForTable_CurrentCellChanged(object sender, EventArgs e)
        {
            ButtonForRemoveRow.IsEnabled = true;
        }

        private void DataGridForTable_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {

        }

        private void DBTreeView_GotFocus(object sender, RoutedEventArgs e)
        {
            ButtonForRemoveRow.IsEnabled = false;
        }
    }
}
