using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.Controls;

namespace GraphPG
{
    enum DataGridContent
    {
        ContentGrid,
        SchemaGrid
    }

    class Controller
    {
        public static Controller GetController()
        {
            return controller;
        }

        public DataGridContent DGContent { get; set; }

        public bool ConnectDB(string connectString, out string resString)
        {
            _connection = new NpgsqlConnection(connectString);

            try
            {
                _connection.Open();
            }
            catch (Exception ex)
            {
                resString = "Fail to connect." + ex.Message;
                return false;
            }

            resString = "Connection is built successfully.";
            GenerateDBTree();
            return true;
        }

        public void SetMainWindow(MainWindow window)
        {
            this._mainWindow = window;
            string s;
            ConnectDB("Host=localhost;Username=postgres;Password=527452zg666;Database=whu", out s);
        }

        public void OpenTable(string DBName, string TBName)
        {
            if (DGContent == DataGridContent.ContentGrid)
            {
                string cmdStr = "select * from " + TBName;
                using (NpgsqlCommand cmd = new NpgsqlCommand(cmdStr, _connection))
                {
                    _adapter = new NpgsqlDataAdapter(cmd);
                    _dataTable = new DataTable();
                    _adapter.Fill(_dataTable);
                }

                _mainWindow.DataGridForContent.ItemsSource = _dataTable.DefaultView;
            }
            else
            {
                string cmdStr = string.Format("select column_name, data_type from information_schema.columns where table_name='{0}'", TBName);
                using (NpgsqlCommand cmd = new NpgsqlCommand(cmdStr, _connection))
                {
                    _adapter = new NpgsqlDataAdapter(cmd);
                    _dataTable = new DataTable(TBName);
                    _adapter.Fill(_dataTable);
                    _dataTable.AcceptChanges();
                }

                _mainWindow.DataGridForSchema.ItemsSource = _dataTable.DefaultView;
            }
        }

        public void UpdateTable()
        {
            if (DGContent == DataGridContent.ContentGrid)
            {
                NpgsqlCommandBuilder builder = new NpgsqlCommandBuilder(_adapter);
                try
                {
                    _adapter.Update(_dataTable);
                }
                catch (Exception ex)
                {
                    _mainWindow.ShowMessageAsync("error", ex.Message);
                    _mainWindow.LabelForStatus.Content = ex.Message;
                    _mainWindow.LabelForStatus.Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 0));
                    RestoreTable();
                }
            }
            else
            {
                var changes = _dataTable.GetChanges();
                string cmdStr = "";
                using (NpgsqlCommand cmd = new NpgsqlCommand())
                {
                    foreach (DataRow dr in changes.Rows)
                    {
                        if (dr.RowState == DataRowState.Modified)
                        {
                            foreach (DataColumn dc in changes.Columns)
                            {
                                if (dc.ColumnName.Equals("column_name"))
                                {
                                    cmdStr = string.Format("alter table {0} rename column {1} to {2}", _dataTable.TableName,
                                                            dr[dc, DataRowVersion.Original], dr[dc, DataRowVersion.Current]);
                                    cmd.CommandText = cmdStr;
                                    cmd.Connection = _connection;
                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                }

            }

        }

        public void RestoreTable()
        {
            _mainWindow.DataGridForContent.ItemsSource = null;
            _dataTable.Clear();
            _adapter.Fill(_dataTable);
            _mainWindow.DataGridForContent.ItemsSource = _dataTable.DefaultView;
        }

        private void GenerateDBTree()
        {
            TreeViewItem root = new TreeViewItem();
            root.Header = _connection.Database;
            root.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            root.MouseLeftButtonUp += _mainWindow.Tree_DB_MouseLeftButtonUp;
            _mainWindow.DBTreeView.Items.Add(root);

            NpgsqlCommand command = new NpgsqlCommand("select tablename from pg_tables", _connection);
            NpgsqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                TreeViewItem table = new TreeViewItem();
                table.Header = reader["tablename"];
                table.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
                table.MouseLeftButtonUp += _mainWindow.Tree_Table_MouseLeftButtonUp;
                root.Items.Add(table);
            }
            reader.Close();
            command.Dispose();
        }


        private Controller()
        {
        }
        private static readonly Controller controller = new Controller();
        private NpgsqlConnection _connection;
        private NpgsqlDataAdapter _adapter;
        private DataTable _dataTable;
        private MainWindow _mainWindow;
    }
}
