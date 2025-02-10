using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.Collections.Concurrent;

using ServiceLib;

namespace ServerLib
{
    class Server
    {
        private List<ClientData> clients;

        private BackgroundService service; // the servis that run in the background
        private TcpListener listener; // if there is connection that var catch it

        private string host; // the host pc
        private int port; // the port

        // builders
        public Server(string host, int port)
        {
            this.host = host;
            this.port = port;

            Init();
        }

        // read the name...
        private void Init()
        {
            clients = new List<ClientData>();
            listener = new TcpListener(IPAddress.Parse(host), port);
            service = new BackgroundService(TimeSpan.FromMilliseconds(100));

            service.Add(new CustomAction(new Action(() => ClientHunt()), TimeSpan.FromMilliseconds(500)));

            Start();
        }

        // basic cmd
        public void Start()
        {
            listener.Start(); // start to lis
            service.Start();
        }
        public void Stop()
        {
            listener.Stop();
            service.Stop();
        }

        // Handling with clients
        private void ClientHunt()
        {
            try
            {
                TcpClient client = listener.AcceptTcpClient();
                BackgroundService.Run(() => AddClient(client));
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
        }
        private void AddClient(TcpClient client)
        {
            ClientData newClient = ReceiveClientData(client);
            Console.WriteLine($"{newClient.name} has been Connected! from: {newClient.ip} level: {newClient.level}");

            newClient.id = service.Add(new CustomAction(new Action(() => ClientListener(newClient)), TimeSpan.FromMilliseconds(150)));
            SendData(newClient ,"Welcome to Net42");
            clients.Add(newClient);

            chat($"Server: New user has been join: {newClient.name}", newClient.client);
        }
        private void RemoveClient(ClientData client) // need fix
        {
            clients.Remove(client);
            service.Remove(client.id);

            chat($"Server: {client.name} Disconnected ", client.client);
        }
        private void ClientListener(ClientData client)
        {
            if (!ClientConnection(client))
            {
                return;
            }

            Console.WriteLine("Listen for: " + client.name);

            string data = ReceiveData(client);
            if (data != null)
            {
                DataProcessor(client, data);
            }
        }
        private void DataProcessor(ClientData client,string data)
        {
            Console.WriteLine($"{client.name} As send: {data}");

            chat(client.name + ": " + data, client.client);
            //chat(client.name + ": " + data);
        }

        // client interaction
        private ClientData ReceiveClientData(TcpClient client)
        {
            string[] data = ReceiveData(client).Split('/');

            return new ClientData(client, data[0], data[1], int.Parse(data[2]), -1);
        }
        private string ReceiveData(ClientData user)
        {
            if (!ClientConnection(user))
            {
                return null;
            }

            NetworkStream stream = user.client.GetStream();

            byte[] bytes = new byte[256];

            int bytesRead = 0;
            try
            {
                bytesRead = stream.Read(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Client {user.name} has probabli disconnected:" + ex.Message);
            }

            if (bytesRead == 0)
            {
                return null;
            }
            return Encoding.ASCII.GetString(bytes, 0, bytesRead);
        }
        private string ReceiveData(TcpClient client) // only once
        {
            if (!ClientConnection(client))
            {
                return null;
            }

            NetworkStream stream = client.GetStream();

            byte[] bytes = new byte[256];

            int bytesRead = 0;
            try
            {
                bytesRead = stream.Read(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Client has probabli disconnected:" + ex.Message);
            }

            if (bytesRead == 0)
            {
                return null; // or throw an exception, depending on your requirements
            }
            return Encoding.ASCII.GetString(bytes, 0, bytesRead);
        }
        private void SendData(ClientData user, string data)
        {
            if (!ClientConnection(user))
            {
                return;
            }

            NetworkStream stream = user.client.GetStream();
            try
            {
                byte[] msg = Encoding.ASCII.GetBytes(data);
                stream.Write(msg, 0, msg.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Client {user.name} has probabli disconnected at poit 1:" + ex.Message);
            }
        }
        private bool ClientConnection(ClientData user)
        {
            if (!user.client.Connected)
            {
                RemoveClient(user);
                Console.WriteLine($"Client {user.id} has disconnected ");
                return false; // Client has disconnected
            }
            return true;
        }
        private bool ClientConnection(TcpClient client)
        {
            if (!client.Connected)
            {
                Console.WriteLine($"Client has disconnected ");
                return false; // Client has disconnected
            }
            return true;
        }


        // others
        private void chat(string data)
        {
            foreach (ClientData client in clients)
            {
                SendData(client, data);
                //SendData(client, data.ToUpper());
            }
        }
        private void chat(string data, TcpClient sender)
        {
            foreach (ClientData client in clients)
            {
                if (client.client == sender)
                {
                    continue;
                }

                SendData(client, data);
                //SendData(client, data.ToUpper());
            }
        }
    }
    public struct ClientData
    {
        // vars
        private TcpClient _client;
        private string _name;
        private string _ip;
        private int _level;
        private int _id;

        // builders
        public ClientData(TcpClient client, string name, string ip, int level, int id)
        {
            this._client = client;
            this._name = name;
            this._ip = ip;
            this._level = level;
            this._id = id;
        }
        public ClientData(ClientData clientData)
        {
            this._client = clientData.client;
            this._name = clientData.name;
            this._ip = clientData.ip;
            this._level = clientData.level;
            this._id = clientData.id;
        }

        // getters + setters
        public TcpClient client
        {
            get
            {
                return _client;
            }

            set
            {
                _client = value;
            }
        }
        public string name
        {
            get
            {
                return _name;
            }

            set
            {
                _name = value;
            }
        }
        public string ip
        {
            get
            {
                return _ip;
            }

            set
            {
                _ip = value;
            }
        }
        public int level
        {
            get
            {
                return _level;
            }

            set
            {
                _level = value;
            }
        }
        public int id
        {
            get
            {
                return _id;
            }

            set
            {
                _id = value;
            }
        }
    }
}
