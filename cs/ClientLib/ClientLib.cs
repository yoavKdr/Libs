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

namespace ClientLib
{
    class Client
    {
        private BackgroundService service;

        private ClientData clientData; // there is somone who lisen(server) and there is other who scream(client)
        private NetworkStream stream;    // the stream of the transfer info
        private string host;
        private int port;

        // builders
        public Client(string host, int port)
        {
            this.host = host;
            this.port = port;

            Init();
        }

        // getters + setters
        public ClientData ClientData
        {
            get
            {
                return clientData;
            }
        }
        // read the function name...
        private void Init()
        {
            service = new BackgroundService(TimeSpan.FromMilliseconds(100));

            clientData = new ClientData(new TcpClient(), "", "", 0, -1);

            string name;
            while (true)
            {
                Console.Write("Enter your name: ");
                name = Console.ReadLine();
                if (Regex.IsMatch(name, @"^[a-zA-Z]+$"))
                {
                    break; // name is valid, exit the loop
                }
                else
                {
                    Console.Clear();
                    Console.WriteLine("Invalid name. Please enter only letters.");
                }
            }
            clientData.name = name;

            while (true)
            {
                try
                {
                    ClientData.client.Connect(host, port);
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error:" + ex.Message);
                }
            }
            stream = clientData.client.GetStream();

            IPEndPoint endpoint = (IPEndPoint)clientData.client.Client.RemoteEndPoint;
            clientData.ip = endpoint.Address.ToString();

            Console.Clear();

            SendData(clientData.name + "/" + clientData.ip + "/" + clientData.level); // send the client data

            service.Add(new CustomAction(new Action(() => ListenToTheServer()), TimeSpan.FromMilliseconds(120)));
            service.Start();
        }

        // basic cmd
        public void Close()
        {
            clientData.client.Close();
        }

        // Handling with the server
        private void ListenToTheServer()
        {
            string data = RecieveData();
            if (data != null)
            {
                SaveLine(data);
            }
        }

        // server interaction
        public void SendData(string data)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(data);   // string to ascii
            stream.Write(bytes, 0, bytes.Length);   // Send the ascii to the server
            //Console.WriteLine($"Sent: {data}");
        }
        public string RecieveData()
        {
            int bufferSize = 256;    // the name speak for him self
            byte[] bytes = new byte[bufferSize];    // Buffer for the data

            int bytesRead = stream.Read(bytes, 0, bytes.Length);    // Read the data
            string data = Encoding.ASCII.GetString(bytes, 0, bytesRead); // ascii to string
            //Console.WriteLine($"Recieve: {data}");

            return data;
        }
        public void SaveLine(string data)
        {
            int cursorTop = Console.CursorTop;
            int cursorLeft = Console.CursorLeft;

            Console.SetCursorPosition(0, cursorTop);
            string line = "";
            while (true)
            {
                ConsoleKeyInfo cki = Console.ReadKey(true);
                if (cki.Key == ConsoleKey.Enter)
                    break;
                line += cki.KeyChar;
            }
            Console.SetCursorPosition(cursorLeft, cursorTop);

            Console.WriteLine(data);
            Console.WriteLine(line);
            Console.SetCursorPosition(cursorLeft, cursorTop - 1);
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
