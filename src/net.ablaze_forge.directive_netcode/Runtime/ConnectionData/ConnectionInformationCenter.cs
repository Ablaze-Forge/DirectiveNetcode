using System.Collections.Concurrent;

namespace AblazeForge.DirectiveNetcode.ConnectionData
{
    public interface IConnectionInformationProvider
    {
        public bool RegisterConnection(ulong uid, ushort connectionStatus = 0);
        public bool RemoveConnection(ulong uid);
        public bool GetConnectionInformation(ulong uid, out ConnectionInformation connectionInformation);
        public bool ConnectionMeetsPermissionCriteria(ulong uid, ushort requiredFlags);
    }

    public class ServerDefaultConnectionInformationProvider : IConnectionInformationProvider
    {
        private readonly ConcurrentDictionary<ulong, ConnectionInformation> m_ConnectionInformationList = new();

        public bool RegisterConnection(ulong uid, ushort connectionStatus = 0)
        {
            return m_ConnectionInformationList.TryAdd(uid, new ConnectionInformation(uid, connectionStatus));
        }

        public bool RemoveConnection(ulong uid)
        {
            return m_ConnectionInformationList.TryRemove(uid, out ConnectionInformation _);
        }

        public bool GetConnectionInformation(ulong uid, out ConnectionInformation connectionInformation)
        {
            return m_ConnectionInformationList.TryGetValue(uid, out connectionInformation);
        }

        public bool ConnectionMeetsPermissionCriteria(ulong uid, ushort requiredFlags)
        {
            if (!m_ConnectionInformationList.TryGetValue(uid, out ConnectionInformation connectionInformation))
            {
                return false;
            }

            return connectionInformation.Status.MeetsCriteria(requiredFlags);
        }
    }

    public class ClientDefaultConnectionInformationProvider : IConnectionInformationProvider
    {
        public ConnectionInformation MyInformation;

        private readonly ConcurrentDictionary<ulong, ConnectionInformation> m_ConnectionInformationList = new();

        public bool RegisterConnection(ulong uid, ushort connectionStatus = 0)
        {
            if (uid == 0)
            {
                MyInformation = new ConnectionInformation(uid, connectionStatus);

                return true;
            }

            return m_ConnectionInformationList.TryAdd(uid, new ConnectionInformation(uid, connectionStatus));
        }

        public bool RemoveConnection(ulong uid)
        {
            if (uid == 0)
            {
                MyInformation = null;
                return true;
            }

            return m_ConnectionInformationList.TryRemove(uid, out ConnectionInformation _);
        }

        public bool GetConnectionInformation(ulong uid, out ConnectionInformation connectionInformation)
        {
            if (uid == 0)
            {
                if (MyInformation == null)
                {
                    connectionInformation = null;
                    return false;
                }

                connectionInformation = MyInformation;
                return true;
            }

            return m_ConnectionInformationList.TryGetValue(uid, out connectionInformation);
        }

        public bool ConnectionMeetsPermissionCriteria(ulong uid, ushort requiredFlags)
        {
            if (uid == 0)
            {
                if (MyInformation == null)
                {
                    return false;
                }

                return MyInformation.Status.MeetsCriteria(requiredFlags);
            }

            if (!m_ConnectionInformationList.TryGetValue(uid, out ConnectionInformation connectionInformation))
            {
                return false;
            }

            return connectionInformation.Status.MeetsCriteria(requiredFlags);
        }
    }
}
