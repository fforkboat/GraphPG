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

        private void DBTreeView_GotFocus(object sender, RoutedEventArgs e)
        {
            ButtonForRemoveRow.IsEnabled = false;
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
            GridForAddAndRemoveRow.Visibility = Visibility.Visible;
            DataGridForContent.Visibility = Visibility.Visible;
            DataGridForSchema.Visibility = Visibility.Collapsed;
            ButtonForModifyColumn.IsEnabled = true;

            var table = (TreeViewItem)sender;
            var database = (TreeViewItem)table.Parent;
            _openTableName = table.Header.ToString();
            _openDBName = table.Header.ToString();

            Controller.GetController().DGContent = DataGridContent.ContentGrid;
            Controller.GetController().OpenTable(_openDBName, _openTableName);
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
            string a = (DataGridForContent.ItemsSource as DataView).Table.Rows[e.Row.GetIndex()][e.Column.DisplayIndex].ToString();
            if ((e.EditingElement as TextBox).Text.Equals(a))
                return;

            OperationInModifingTable();
        }

        private void DataGridForTable_CurrentCellChanged(object sender, EventArgs e)
        {
            ButtonForRemoveRow.IsEnabled = true;
        }

        private void ButtonForSaveChanges_Click(object sender, RoutedEventArgs e)
        {
            Controller.GetController().UpdateTable();

            OperationAfterModifingTable();
        }

        private void ButtonForCancelChanges_Click(object sender, RoutedEventArgs e)
        {
            Controller.GetController().RestoreTable();

            OperationAfterModifingTable();
        }

        private void ButtonForAddRow_Click(object sender, RoutedEventArgs e)
        {
            OperationInModifingTable();

            var dataView = DataGridForContent.ItemsSource as DataView;
            dataView.AddNew();
            for (int i = 0; i < dataView.Count - 1 ; i++)
            {
                var row = DataGridForContent.ItemContainerGenerator.ContainerFromIndex(i) as DataGridRow;
                if (row == null)
                {
                    DataGridForContent.UpdateLayout();
                    DataGridForContent.ScrollIntoView(DataGridForContent.Items[i]);
                    row = DataGridForContent.ItemContainerGenerator.ContainerFromIndex(i) as DataGridRow;
                }
                row.IsEnabled = false;
                row.IsSelected = false; 
            }

            var newRow = DataGridForContent.ItemContainerGenerator.ContainerFromIndex(dataView.Count - 1) as DataGridRow;
            if (newRow == null)
            {
                DataGridForContent.UpdateLayout();
                DataGridForContent.ScrollIntoView(DataGridForContent.Items[dataView.Count - 1]);
                newRow = DataGridForContent.ItemContainerGenerator.ContainerFromIndex(dataView.Count - 1) as DataGridRow;
            }
            newRow.IsSelected = true;
        }

        private void ButtonForRemoveRow_Click(object sender, RoutedEventArgs e)
        {
            OperationInModifingTable();

            var dataView = DataGridForContent.ItemsSource as DataView;
            dataView.Delete(DataGridForContent.SelectedIndex);
        }

        private void StatusLabel_MouseLeave(object sender, MouseEventArgs e)
        {
            LabelForStatus.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
        }

        private void OperationInModifingTable()
        {
            DataGridForContent.CellEditEnding -= DataGridForTable_CellEditEnding;
            GridForModifyTableAction.Visibility = Visibility.Visible;
            DBTreeView.IsEnabled = false;
            ToolBarMain.IsEnabled = false;
            GridForAddAndRemoveRow.IsEnabled = false;
        }

        private void OperationAfterModifingTable()
        {
            GridForModifyTableAction.Visibility = Visibility.Collapsed;
            DBTreeView.IsEnabled = true;
            ToolBarMain.IsEnabled = true;
            GridForAddAndRemoveRow.IsEnabled = true;
            DataGridForContent.CellEditEnding += DataGridForTable_CellEditEnding;

            var dataView = DataGridForContent.ItemsSource as DataView;
            for (int i = 0; i < dataView.Count; i++)
            {
                var row = DataGridForContent.ItemContainerGenerator.ContainerFromIndex(i) as DataGridRow;
                if (row == null)
                {
                    DataGridForContent.UpdateLayout();
                    DataGridForContent.ScrollIntoView(DataGridForContent.Items[i]);
                    row = DataGridForContent.ItemContainerGenerator.ContainerFromIndex(i) as DataGridRow;
                }
                row.IsEnabled = true;
                row.IsSelected = false;
            }
            ButtonForRemoveRow.IsEnabled = false;
        }

        private void ButtonForModifyColumn_Click(object sender, RoutedEventArgs e)
        {
            DataGridForContent.Visibility = Visibility.Collapsed;
            DataGridForSchema.Visibility = Visibility.Visible;

            Controller.GetController().DGContent = DataGridContent.SchemaGrid;
            Controller.GetController().OpenTable(_openDBName, _openTableName);
        }

        private string _openTableName;
        private string _openDBName;
    }
}
