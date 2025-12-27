using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NModbus;

namespace HalconWinFormsDemo.Services
{
    public class ModbusPlcService : IDisposable
    {
        private readonly object sendLock = new();
        private readonly object connLock = new();
        private TcpClient tcp;
        private IModbusMaster master;
        private volatile bool connected;

        public bool IsConnected => connected;

        public string Name { get; }
        public string Ip { get; private set; }
        public int Port { get; private set; } = 502;
        public byte SlaveId { get; private set; } = 1;

        public event Action<bool> ConnectionStateChanged;

        public ModbusPlcService(string name)
        {
            Name = name;
        }

        public void Configure(string ip, byte slaveId, int port = 502)
        {
            Ip = NormalizeHost(ip);
            SlaveId = slaveId;
            Port = port;
        }

        private static string NormalizeHost(string host)
        {
            if (host == null) return string.Empty;
            // Remove embedded nulls and trim.
            host = host.Replace("\0", string.Empty).Trim();
            return host;
        }

        private bool IsConfiguredAndValid()
        {
            if (Port <= 0) return false;
            if (string.IsNullOrWhiteSpace(Ip)) return false;
            // Accept both IP and DNS names; prefer IP validation when possible.
            if (IPAddress.TryParse(Ip, out _)) return true;
            // If not an IP, require non-empty and no whitespace.
            return Ip.IndexOfAny(new[] { ' ', '\t', '\r', '\n' }) < 0;
        }

        public bool TestConnection(int timeoutMs = 1000)
        {
            try
            {
                if (!IsConfiguredAndValid())
                    return false;

                using var c = new TcpClient();
                var task = c.ConnectAsync(Ip, Port);
                return task.Wait(timeoutMs) && c.Connected;
            }
            catch
            {
                return false;
            }
        }

        public void EnsureConnected()
        {
            // Never throw from this method; PLC is optional until configured.
            try
            {
                lock (connLock)
                {
                    if (connected) return;

                    if (!IsConfiguredAndValid())
                    {
                        connected = false;
                        ConnectionStateChanged?.Invoke(false);
                        return;
                    }

                    Disconnect();

                    tcp = new TcpClient();
                    // ConnectAsync may throw synchronously for invalid hosts on some .NET Framework builds.
                    var task = tcp.ConnectAsync(Ip, Port);
                    if (!task.Wait(1000))
                    {
                        Disconnect();
                        ConnectionStateChanged?.Invoke(false);
                        return;
                    }

                    var factory = new ModbusFactory();
                    master = factory.CreateMaster(tcp);
                    master.Transport.Retries = 0;
                    master.Transport.ReadTimeout = 1000;
                    master.Transport.WriteTimeout = 1000;

                    connected = true;
                    ConnectionStateChanged?.Invoke(true);
                }
            }
            catch
            {
                Disconnect();
                ConnectionStateChanged?.Invoke(false);
            }
        }

        public void SetAlarmRegister(ushort reg0Based, bool on)
        {
            Task.Run(() =>
            {
                try
                {
                    EnsureConnected();
                    if (!connected) return;

                    lock (sendLock)
                    {
                        master.WriteSingleRegister(SlaveId, reg0Based, (ushort)(on ? 1 : 0));
                    }
                }
                catch
                {
                    Disconnect();
                    ConnectionStateChanged?.Invoke(false);
                }
            });
        }

        public void Disconnect()
        {
            connected = false;
            try { tcp?.Close(); } catch { }
            tcp = null;
            master = null;
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
