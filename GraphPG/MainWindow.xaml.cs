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
using MahApps.Metro.Controls.Dialogs;
using MaterialDesignThemes.Wpf;

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
            Controller.GetController().LoadSavedConnections();

            DataGridComboxColumnForSchema.ItemsSource = new List<string> { "character varying", "integer", "float"};
        }

        private void DBTreeView_GotFocus(object sender, RoutedEventArgs e)
        {
            ButtonForRemoveRow.IsEnabled = false;
        }
        public void Tree_DB_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _openDBName = (sender as TreeViewItem).Header.ToString();
            Controller.GetController().SetCurrentConnection(_openDBName);

            ButtonForDisConnect.IsEnabled = true;
            ButtonForDropDB.IsEnabled = true;
            ButtonForCreateTable.IsEnabled = true;

            TreeViewItem treeItem = (TreeViewItem)sender;
            if (treeItem == null || e.Handled)
                return;
            treeItem.IsExpanded = !treeItem.IsExpanded;

            e.Handled = true;
        }

        public void Tree_Table_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _openTableName = (sender as TreeViewItem).Header.ToString();
            _openDBName = ((sender as TreeViewItem).Parent as TreeViewItem).Header.ToString();
            Controller.GetController().SetCurrentConnection(_openDBName);

            GridForAddAndRemoveRow.Visibility = Visibility.Visible;
            DataGridForContent.Visibility = Visibility.Visible;
            DataGridForSchema.Visibility = Visibility.Collapsed;
            ButtonForModifyColumn.IsEnabled = true;
            ButtonForDeleteTable.IsEnabled = true;
            ButtonForDisConnect.IsEnabled = true;
            ButtonForDropDB.IsEnabled = true;

            Controller.GetController().DGContent = DataGridContent.ContentGrid;
            Controller.GetController().OpenTable(_openDBName, _openTableName);
            e.Handled = true;
        }

        private void Button_ConnectDB_Click(object sender, RoutedEventArgs e)
        {
            var connectForm = new ConnectWindow(this);
            connectForm.ShowDialog();
        }

        private void OperationForRemoveDBTreeViewItem()
        {
            GridForAddAndRemoveRow.Visibility = Visibility.Collapsed;
            ButtonForDisConnect.IsEnabled = false;
            ButtonForModifyColumn.IsEnabled = false;
            ButtonForDeleteTable.IsEnabled = false;
            ButtonForCreateTable.IsEnabled = false;
            ButtonForDropDB.IsEnabled = false;
            DataGridForContent.ItemsSource = null;
        }

        private void ButtonForDisconnect_Click(object sender, RoutedEventArgs e)
        {
            Controller.GetController().DisconnectCurrentDB();

            var DBRoot = DBTreeView.FindChild<TreeViewItem>(_openDBName);
            DBTreeView.Items.Remove(DBRoot);

            OperationForRemoveDBTreeViewItem();

        }

        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            var datagrid = sender as DataGrid;
            string originalValue = (datagrid.ItemsSource as DataView).Table.Rows[e.Row.GetIndex()][e.Column.DisplayIndex].ToString();

            if (e.EditingElement is ComboBox && (e.EditingElement as ComboBox).Text.Equals(originalValue))
                return;
            if (e.EditingElement is TextBox && (e.EditingElement as TextBox).Text.Equals(originalValue))
                return;
            if (e.EditingElement is NumericUpDown && (e.EditingElement as NumericUpDown).Value.ToString().Equals(originalValue))
                return;
            if (e.EditingElement is CheckBox)
            {
                if ((originalValue == "YES" && (bool)(e.EditingElement as CheckBox).IsChecked) || (originalValue == "NO" && !(bool)(e.EditingElement as CheckBox).IsChecked))
                    return;
            }
                
            OperationInModifingTable();
        }

        private void DataGrid_CurrentCellChanged(object sender, EventArgs e)
        {
            ButtonForRemoveRow.IsEnabled = true;
        }

        private void ButtonForSaveChanges_Click(object sender, RoutedEventArgs e)
        {
            Controller.GetController().UpdateCurrentTable();

            OperationAfterModifingTable();
        }

        private void ButtonForCancelChanges_Click(object sender, RoutedEventArgs e)
        {
            Controller.GetController().RestoreCurrentTable();

            OperationAfterModifingTable();
        }

        private void ButtonForAddRow_Click(object sender, RoutedEventArgs e)
        {
            OperationInModifingTable();

            if (Controller.GetController().DGContent == DataGridContent.ContentGrid)
            {
                DataView dataView = DataGridForContent.ItemsSource as DataView;
                dataView.AddNew();

                for (int i = 0; i < dataView.Count - 1; i++)
                {
                    var row = DataGridForContent.GetRow(i);
                    row.IsEnabled = false;
                    row.IsSelected = false;
                }

                var newRow = DataGridForContent.GetRow(dataView.Count - 1);
                newRow.IsSelected = true;
            }

            else
            {
                DataView dataView = DataGridForSchema.ItemsSource as DataView;
                dataView.AddNew();

                for (int i = 0; i < dataView.Count - 1; i++)
                {
                    DataGridRow row = DataGridForSchema.GetRow(i);
                    row.IsEnabled = false;
                    row.IsSelected = false;
                }

                DataGridRow newRow = DataGridForSchema.GetRow(dataView.Count - 1);
                newRow.IsSelected = true;

            }
        }

        private void ButtonForRemoveRow_Click(object sender, RoutedEventArgs e)
        {
            OperationInModifingTable();

            DataView dataView;
            if (Controller.GetController().DGContent == DataGridContent.ContentGrid)
            {
                dataView = DataGridForContent.ItemsSource as DataView;

                for (int i = dataView.Count - 1; i >= 0; i--)
                {
                    var row = DataGridForContent.GetRow(i);
                    if (row.IsSelected)
                    {
                        dataView.Delete(i);
                    }
                }
            }
            else
            {
                dataView = DataGridForSchema.ItemsSource as DataView;

                for (int i = dataView.Count - 1; i >= 0; i--)
                {
                    var row = DataGridForSchema.GetRow(i);
                    if (row.IsSelected)
                    {
                        dataView.Delete(i);
                    }
                }
            }
        }

        private void StatusLabel_MouseLeave(object sender, MouseEventArgs e)
        {
            LabelForStatus.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
        }

        private void OperationInModifingTable()
        {
            GridForModifyTableAction.Visibility = Visibility.Visible;
            DBTreeView.IsEnabled = false;
            ToolBarMain.IsEnabled = false;
            GridForAddAndRemoveRow.IsEnabled = false;

            if (Controller.GetController().DGContent == DataGridContent.ContentGrid)
            {
                DataGridForContent.CellEditEnding -= DataGrid_CellEditEnding;
            }
            else
            {
                DataGridForSchema.CellEditEnding -= DataGrid_CellEditEnding;
            }
        }

        private void OperationAfterModifingTable()
        {
            GridForModifyTableAction.Visibility = Visibility.Collapsed;
            DBTreeView.IsEnabled = true;
            ToolBarMain.IsEnabled = true;
            GridForAddAndRemoveRow.IsEnabled = true;
            ButtonForRemoveRow.IsEnabled = false;

            if (Controller.GetController().DGContent == DataGridContent.ContentGrid)
            {
                DataGridForContent.CellEditEnding += DataGrid_CellEditEnding;
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
            }
            else
            {
                DataGridForSchema.CellEditEnding += DataGrid_CellEditEnding;
                var dataView = DataGridForSchema.ItemsSource as DataView;
                for (int i = 0; i < dataView.Count; i++)
                {
                    var row = DataGridForSchema.ItemContainerGenerator.ContainerFromIndex(i) as DataGridRow;
                    if (row == null)
                    {
                        DataGridForSchema.UpdateLayout();
                        DataGridForSchema.ScrollIntoView(DataGridForSchema.Items[i]);
                        row = DataGridForSchema.ItemContainerGenerator.ContainerFromIndex(i) as DataGridRow;
                    }

                    row.IsEnabled = true;
                    row.IsSelected = false;
                }
            }
        }

        private void ButtonForModifyColumn_Click(object sender, RoutedEventArgs e)
        {
            DataGridForContent.Visibility = Visibility.Collapsed;
            DataGridForSchema.Visibility = Visibility.Visible;
            ButtonForDeleteTable.IsEnabled = false;

            Controller.GetController().DGContent = DataGridContent.SchemaGrid;
            Controller.GetController().OpenTable(_openDBName, _openTableName);
        }

        private string _openTableName;
        private string _openDBName;

//         private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
//         {
//             var dataGridCell = (sender as FrameworkElement).Parent as DataGridCell;
//            
//             var dataGridRow = dataGridCell.Parent as DataGridRow;
// 
//             if (e.Source.ToString() == "character varying")
//             {
//                 DataGridForSchema.GetCellContent(dataGridRow.GetIndex(), 2).IsEnabled = true;
//             }
//             else
//             {
//                 DataGridForSchema.GetCellContent(dataGridRow.GetIndex(), 2).IsEnabled = false;
//             }
//         }
// 
//         private void ButtonForDisablingCheckbox_Click(object sender, RoutedEventArgs e)
//         {
//             // The following line can reach the purpose of disabling the control in the cell
//             // DataGridForSchema.GetCell(0, 3).IsEnabled = false;
// 
//             // The following line can not reach that purpose
//             (DataGridForSchema.GetCell(0, 3).Content as CheckBox).IsEnabled = false;
// 
//         }

        private void ButtonForDropTable_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ConfirmationDialog(this, "Be sure to drop the table: " + _openTableName);
            dialog.ShowDialog();

            if (dialog.IsConfirm)
            {
                if (!Controller.GetController().DropCurrentTable())
                    return;

                var db = DBTreeView.FindChild<TreeViewItem>(_openDBName);
                var table = DBTreeView.FindChild<TreeViewItem>(_openDBName + "_" + _openTableName);
                db.Items.Remove(table);

                GridForAddAndRemoveRow.Visibility = Visibility.Collapsed;
                ButtonForModifyColumn.IsEnabled = false;
                ButtonForDeleteTable.IsEnabled = false;
                DataGridForContent.ItemsSource = null;
            }
        }

        private void ButtonForDropDB_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ConfirmationDialog(this, "Be sure to drop the database: " + _openDBName);
            dialog.ShowDialog();

            if (dialog.IsConfirm)
            {
                if (!Controller.GetController().DropCurrentDB())
                    return;

                var db = DBTreeView.FindChild<TreeViewItem>(_openDBName);
                DBTreeView.Items.Remove(db);

                OperationForRemoveDBTreeViewItem();
            }
        }

        private void ButtonForCreateTable_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
