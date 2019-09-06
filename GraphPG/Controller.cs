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
using System.Windows.Data;
using System.Configuration;
using System.Xml;
using System.IO;

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

        public bool ConnectDB(string connectionStr, out string resString, ConnectionSettings settings)
        {
            NpgsqlConnection connection = new NpgsqlConnection(connectionStr);
            try
            {
                connection.Open();
                _connections.Add(connection.Database, connection);
            }
            catch (Exception ex)
            {
                resString = "Fail to connect: " + ex.Message;
                connection.Dispose();

                return false;
            }

            if (settings.IsAutomaticallyOpen)
            {
                XmlDocument xml = new XmlDocument();
                xml.Load("connection.xml");

                var root = xml.GetElementsByTagName("connectionStrings")[0];
                var node = xml.CreateElement(connection.Database);
                node.InnerText = connectionStr + "/" + settings.IsShowSystemTables.ToString();
                root.AppendChild(node);

                xml.Save("connection.xml");
            }

            GenerateDBTree(connection.Database, settings.IsShowSystemTables);

            resString = "Connection is built successfully.";
            return true;
        }
        public bool ConnectDB(string connectionStr, bool isShowSystemTables)
        {
            NpgsqlConnection connection = new NpgsqlConnection(connectionStr);

            try
            {
                connection.Open();
                _connections.Add(connection.Database, connection);
            }
            catch (Exception)
            {
                XmlDocument xml = new XmlDocument();
                xml.Load("connection.xml");
                var root = xml.SelectSingleNode("/connectionStrings");
                var node = xml.SelectSingleNode("/connectionStrings/" + connection.Database);
                root.RemoveChild(node);
                xml.Save("connection.xml");

                connection.Dispose();
                return false;
            }

            GenerateDBTree(connection.Database, isShowSystemTables);
            return true;
        }

        public void DisconnectCurrentDB()
        {
            _connections.Remove(_currentConnection.Database);
        }

        public void SetMainWindow(MainWindow window)
        {
            this._mainWindow = window;
        }

        public void SetCurrentConnection(string DBName)
        {
            _currentConnection = _connections[DBName];
        }

        private void SetEnableStateForMaxCharLengthCol()
        {
            for (int i = 0; i < _dataTable.Rows.Count; i++)
            {
                DataRow dataRow = _dataTable.Rows[i];
                if (dataRow[1].ToString() != "character varying")
                {
                    // 得到character_maximum那一列中的cell的输入控件，并把它disable
                    var control = _mainWindow.DataGridForSchema.GetCellContent(i, 2);
                    control.IsEnabled = false;
                }
            }
        }

        public void OpenTable(string DBName, string TName)
        {
            try
            {
                if (DGContent == DataGridContent.ContentGrid)
                {
                    string cmdStr = "select * from " + TName;
                    NpgsqlCommand cmd = new NpgsqlCommand(cmdStr, _currentConnection);
                    _adapter = new NpgsqlDataAdapter(cmd);
                    _dataTable = new DataTable(TName);
                    _adapter.Fill(_dataTable);

                    _mainWindow.DataGridForContent.ItemsSource = _dataTable.DefaultView;
                }
                else
                {
                    string cmdStr = string.Format("select column_name, data_type, character_maximum_length, is_nullable from information_schema.columns where table_name='{0}'", TName);
                    NpgsqlCommand cmd = new NpgsqlCommand(cmdStr, _currentConnection);
                    _adapter = new NpgsqlDataAdapter(cmd);
                    _dataTable = new DataTable(TName);
                    _adapter.Fill(_dataTable);
                    _dataTable.AcceptChanges();

                    _mainWindow.DataGridForSchema.ItemsSource = _dataTable.DefaultView;

                    SetEnableStateForMaxCharLengthCol();

                }
            }
            catch (Exception ex)
            {
                _mainWindow.ShowMessageAsync("error", ex.Message);
                _mainWindow.LabelForStatus.Content = ex.Message;
                _mainWindow.LabelForStatus.Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 0));
            }

        }

        public void UpdateCurrentTable()
        {
            if (DGContent == DataGridContent.ContentGrid)
            {
                NpgsqlCommandBuilder builder = new NpgsqlCommandBuilder(_adapter);
                try
                {
                    // 当在tableview中插入新行但是不做出任何修改时，不会在数据库中插入新行（此时updata返回0），但在视图上还是可以看到一个空行，不合理，所以需要restore一下
                    if (_adapter.Update(_dataTable) == 0)
                        RestoreCurrentTable();
                }
                catch (Exception ex)
                {
                    _mainWindow.ShowMessageAsync("error", ex.Message);
                    _mainWindow.LabelForStatus.Content = ex.Message;
                    _mainWindow.LabelForStatus.Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 0));
                    RestoreCurrentTable();
                }
            }
            else
            {
                if (_dataTable.GetChanges() == null)
                {
                    return;
                }

                try
                {
                    using (NpgsqlCommand cmd = new NpgsqlCommand())
                    {
                        string cmdStr = "";

                        foreach (DataRow dr in _dataTable.Rows)
                        {
                            if (dr.RowState == DataRowState.Modified)
                            {
                                foreach (DataColumn dc in _dataTable.Columns)
                                {
                                    if (dr[dc, DataRowVersion.Original] == dr[dc, DataRowVersion.Current])
                                        continue;

                                    if (dc.ColumnName.Equals("column_name"))
                                    {
                                        cmdStr = string.Format("alter table {0} rename column {1} to {2}", _dataTable.TableName,
                                                                dr[dc, DataRowVersion.Original], dr[dc, DataRowVersion.Current]);
                                        cmd.CommandText = cmdStr;
                                        cmd.Connection = _currentConnection;
                                        cmd.ExecuteNonQuery();
                                    }

                                    if (dc.ColumnName.Equals("data_type"))
                                    {
                                        string colName = dr[0].ToString();
                                        cmdStr = string.Format("alter table {0} alter column {1} type {2}", _dataTable.TableName, colName, dr[dc, DataRowVersion.Current]);
                                        cmd.CommandText = cmdStr;
                                        cmd.Connection = _currentConnection;
                                        cmd.ExecuteNonQuery();
                                    }

                                    if (dc.ColumnName.Equals("character_maximum_length"))
                                    {
                                        string colName = dr[0].ToString();
                                        cmdStr = string.Format("alter table {0} alter column {1} type varchar({2})", _dataTable.TableName, colName, dr[dc, DataRowVersion.Current]);
                                        cmd.CommandText = cmdStr;
                                        cmd.Connection = _currentConnection;
                                        cmd.ExecuteNonQuery();
                                    }

                                    if (dc.ColumnName.Equals("is_nullable"))
                                    {
                                        string colName = dr[0].ToString();

                                        if (dr[3].ToString().Equals("YES"))
                                        {
                                            cmdStr = string.Format("alter table {0} alter column {1} drop not null", _dataTable.TableName, colName);
                                        }
                                        else
                                        {
                                            cmdStr = string.Format("alter table {0} alter column {1} set not null", _dataTable.TableName, colName);
                                        }
                                        cmd.CommandText = cmdStr;
                                        cmd.Connection = _currentConnection;
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                            }
                            else if (dr.RowState == DataRowState.Added)
                            {
                                string colName = dr[0].ToString();
                                string colType = dr[1].ToString();
                                cmdStr = string.Format("alter table {0} add column {1} {2}", _dataTable.TableName, colName, colType);
                                cmd.CommandText = cmdStr;
                                cmd.Connection = _currentConnection;
                                cmd.ExecuteNonQuery();
                            }
                            else if (dr.RowState == DataRowState.Deleted)
                            {
                                string colName = dr[0, DataRowVersion.Original].ToString();
                                cmdStr = string.Format("alter table {0} drop column {1}", _dataTable.TableName, colName);
                                cmd.CommandText = cmdStr;
                                cmd.Connection = _currentConnection;
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                    _dataTable.AcceptChanges();
                    SetEnableStateForMaxCharLengthCol();
                }
                catch (Exception ex)
                {
                    _mainWindow.ShowMessageAsync("error", ex.Message);
                    _mainWindow.LabelForStatus.Content = ex.Message;
                    _mainWindow.LabelForStatus.Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 0));
                    RestoreCurrentTable();
                }
            }
        }

        public void RestoreCurrentTable()
        {
            _dataTable.Clear();
            _adapter.Fill(_dataTable);

            if (DGContent == DataGridContent.ContentGrid)
            {
                _mainWindow.DataGridForContent.ItemsSource = null;
                _mainWindow.DataGridForContent.ItemsSource = _dataTable.DefaultView;
            }
            else
            {
                _mainWindow.DataGridForSchema.ItemsSource = null;
                _mainWindow.DataGridForSchema.ItemsSource = _dataTable.DefaultView;

                SetEnableStateForMaxCharLengthCol();
            }
        }

        public bool DropCurrentTable()
        {
            NpgsqlCommand cmd = new NpgsqlCommand();
            try
            {
                string cmdStr = "drop table " + _dataTable.TableName;
                cmd.Connection = _currentConnection;
                cmd.CommandText = cmdStr;
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                _mainWindow.ShowMessageAsync("error", ex.Message);
                _mainWindow.LabelForStatus.Content = ex.Message;
                _mainWindow.LabelForStatus.Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 0));

                return false;
            }
            finally
            {
                cmd.Dispose();
            }

            return true;
        }

        public bool DropCurrentDB()
        {
            if (_currentConnection.Database == "postgres")
            {
                _mainWindow.ShowMessageAsync("error", "Can not drop system database");
                _mainWindow.LabelForStatus.Content = "Can not drop system database";
                _mainWindow.LabelForStatus.Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 0));

                return false;
            }

            string DBName = _currentConnection.Database;
            _currentConnection.Close();
            _currentConnection.Dispose();

            // 通过系统数据库来删除当前打开的数据库
            NpgsqlConnection connection = new NpgsqlConnection("Host=localhost;Username=postgres;Password=527452zg666;Database=postgres");
            connection.Open();
            NpgsqlCommand cmd = new NpgsqlCommand();
            try
            {
                string cmdStr = string.Format("SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '{0}';", DBName);
                cmd.Connection = connection;
                cmd.CommandText = cmdStr;
                cmd.ExecuteNonQuery();

                cmdStr = "drop database " + DBName;
                cmd.CommandText = cmdStr;
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                _mainWindow.ShowMessageAsync("error", ex.Message);
                _mainWindow.LabelForStatus.Content = ex.Message;
                _mainWindow.LabelForStatus.Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 0));

                return false;
            }
            finally
            {
                cmd.Dispose();
                connection.Close();
            }

            _connections.Remove(DBName);

            XmlDocument xml = new XmlDocument();
            xml.Load("connection.xml");
            var root = xml.GetElementsByTagName("connectionStrings")[0];
            var node = xml.SelectSingleNode("/connectionStrings/" + DBName);
            root.RemoveChild(node);
            xml.Save("connection.xml");

            return true;
        }

        private void GenerateDBTree(string DBName, bool isShowSystemTables)
        {
            TreeViewItem root = new TreeViewItem();
            root.Header = DBName;
            root.Name = DBName;
            root.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            root.MouseLeftButtonUp += _mainWindow.Tree_DB_MouseLeftButtonUp;
            _mainWindow.DBTreeView.Items.Add(root);

            NpgsqlCommand command = new NpgsqlCommand("select tablename from pg_tables", _connections[DBName]);
            NpgsqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                string tableName = reader["tablename"].ToString();
                if (!isShowSystemTables && (tableName.Contains("pg_") || tableName.Contains("sql_")))
                {
                    continue;
                }

                TreeViewItem table = new TreeViewItem();
                table.Header = tableName;
                table.Name = DBName + "_" + tableName;
                table.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
                table.MouseLeftButtonUp += _mainWindow.Tree_Table_MouseLeftButtonUp;
                root.Items.Add(table);
            }
            reader.Close();
            command.Dispose();
        }

        public void LoadSavedConnections()
        {
            if (!File.Exists("connection.xml"))
            {
                XmlDocument xml = new XmlDocument();
                XmlNode declaration = xml.CreateXmlDeclaration("1.0", "utf-8", "");
                xml.AppendChild(declaration);

                var root = xml.CreateElement("connectionStrings");
                xml.AppendChild(root);

                try
                {
                    xml.Save("connection.xml");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
            else
            {
                XmlDocument xml = new XmlDocument();
                xml.Load("connection.xml");

                var root = xml.GetElementsByTagName("connectionStrings")[0];
                foreach (XmlNode con in root.ChildNodes)
                {
                    string[] connectionConfigs = con.InnerText.Split('/');
                    string connectionStr = connectionConfigs[0];
                    bool isShowSystemTables = connectionConfigs[1] == "true";

                    ConnectDB(connectionStr, isShowSystemTables);
                }
            }
        }

        private Controller()
        {
        }
        private static readonly Controller controller = new Controller();
        private NpgsqlConnection _currentConnection;
        private NpgsqlDataAdapter _adapter;
        private DataTable _dataTable;
        private MainWindow _mainWindow;
        private readonly Dictionary<string, NpgsqlConnection> _connections = new Dictionary<string, NpgsqlConnection>();
    }
}
