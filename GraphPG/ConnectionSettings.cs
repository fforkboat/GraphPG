using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphPG
{
    class ConnectionSettings
    {
        public ConnectionSettings(bool isShowSystemTables = false, bool isAutomaticallyOpen = false)
        {
            IsAutomaticallyOpen = isAutomaticallyOpen;
            IsShowSystemTables = isShowSystemTables;
        }

        public bool IsShowSystemTables;
        public bool IsAutomaticallyOpen;
    }
}
