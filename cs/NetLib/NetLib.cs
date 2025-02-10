using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.Collections.Concurrent;
using System.Threading;

namespace NetLib
{
    class Info
    {
        public Info()
        {
            sog = null;
            ip = null;
            name = null;
            barkanit = null;
            unit = null;
        }

        //vars
        private string sog; //סוג רכיב
        private string ip; // ip
        private string name; // שם רכיב
        private string barkanit; //ברקנית
        private string unit; // יחדיה


        //get + sets
        public string Sog
        {
            get { return sog; }
            set { sog = value; }
        }
        public string Ip
        {
            get { return ip; }
            set { ip = value; }
        }
        public string Name
        {
            get { return name; }
            set { name = value; }
        }
        public string Barkanit
        {
            get { return barkanit; }
            set { barkanit = value; }
        }
        public string Unit
        {
            get { return unit; }
            set { unit = value; }
        }
    }

    class Addresses
    {
        // xxx.xxx.xxx.xxx
        private string address;
        // section 0.1.2.3
        private int[] section;

        // getters + setters
        public string Address
        {
            get { return address; }
            set
            {
                address = value;
                section = Address2Section(address);
            }
        }
        public int[] Section
        {
            get { return section; }
            set
            {
                section = value;
                address = Section2Address(section, section.Length);
            }
        }

        // builders
        public Addresses AddAddress(string address)
        {
            this.address = address;
            section = Address2Section(address);

            return this;
        }
        public Addresses AddSection(int[] section, int wantedSections)
        {
            this.section = section;
            address = Section2Address(section, wantedSections);

            return this;
        }
        public Addresses AddSection(int[] section)
        {
            AddSection(section, 4);

            return this;
        }

        public Addresses()
        {
            address = "";
            section = new int[4];
        }

        // legal tests
        public bool AddressLigalTest(string address)
        {
            //1.1.1.1
            if (address.Length > 15)
            {
                //ERROR
                return false;
            }

            int beforDotCounter = 0;
            int dotCounter = 0;
            for (int i = 0; i < address.Length; ++i)
            {
                //213.2.1.
                //i v: 0 1 2 3 4 5 6 7
                //bdv: 1 2 3 0 1 0 1 0
                //dcv: 0 0 0 1 1 2 2 3

                if (!char.IsNumber(address[i]) && address[i] != '.')
                {
                    //ERROR
                    return false;
                }


                if (address[i] == '.')
                {
                    if (beforDotCounter == 0 || i + 1 == address.Length)
                    {
                        //ERROR
                        return false;
                    }

                    beforDotCounter = 0;
                    ++dotCounter;
                    continue;
                }

                if (beforDotCounter > 2)
                {
                    //ERROR
                    return false;
                }

                ++beforDotCounter;
            }

            if (dotCounter != 3)
            {
                //ERROR
                return false;
            }

            return true;
        }
        public bool SectionLigalTest(int[] section, int sectionNum)
        {

            if (section.Length != sectionNum)
            {
                //ERROR
                return false;
            }

            if (sectionNum <= 0)
            {
                //ERROR
                return false;
            }
            if (sectionNum > 4)
            {
                //ERROR
                return false;
            }

            if (section[0] == 0)
            {
                //ERROR
                return false;
            }

            for (int i = 0; i < sectionNum; ++i)
            {
                if (section[i] > 255 || section[i] < 0)
                {
                    //ERROR
                    return false;
                }
            }

            return true;
        }
        public bool SectionLigalTest(int[] section)
        {
            return SectionLigalTest(section, 4);
        }

        // convertors
        public int[] Address2Section(string address)
        {
            // 213.2.1.

            if (!AddressLigalTest(address))
            {
                //ERROR
                return null;
            }

            int[] section = new int[4];
            int location = 0;

            for (int i = 0; i < 4; ++i)
            {
                string number = "";

                for (; location < address.Length; ++location)
                {
                    if (address[location] != '.')
                    {
                        number = number + address[location];
                    }
                    else
                    {
                        ++location;
                        break;
                    }
                }
                Console.WriteLine(number);
                section[i] = int.Parse(number);
            }

            return section;
        }
        public string Section2Address(int[] section, int wantedSections)
        {
            if (!SectionLigalTest(section, wantedSections))
            {
                //ERROR
                return "ERROR: ADDRESS E-LEGAL";
            }

            string address = "";

            if (wantedSections < 0)
            {
                wantedSections = 0;
            }
            if (wantedSections > 4)
            {
                wantedSections = 4;
            }


            for (int i = 0; i < wantedSections; ++i)
            {
                address = address + section[i].ToString();
                if (i == 3)
                {
                    continue;
                }
                address = address + '.';
            }

            return address;
        }
        public string Section2Address(int[] section)
        {
            return Section2Address(section, 4);
        }
    }


    class PingBackgroundService
    {
        private List<IpInfo> _ipAddresses;
        private TimeSpan _interval;
        private CancellationTokenSource _cts;
        private List<DisconnetedStatus> priority;

        //getters + setters
        public List<IpInfo> ipAddresses
        {
            get {  return _ipAddresses; }
            set { _ipAddresses = value; }
        }
        public TimeSpan interval
        {
            get {  return _interval; }
            set { _interval = value; }
        }
        public CancellationTokenSource cts
        {
            get { return _cts; }
            set { _cts = value; }
        }
        public List<DisconnetedStatus> Priority
        {
            get { return priority; }
            set { priority = value; }
        }

        // builders
        public PingBackgroundService(List<DisconnetedStatus> priority, TimeSpan _interval)
        {
            this.priority = priority;
            _ipAddresses = new List<IpInfo>();
            this._interval = _interval;
            _cts = new CancellationTokenSource();
        }
        public PingBackgroundService(List<DisconnetedStatus> priority)
        {
            this.priority = priority;
            _ipAddresses = new List<IpInfo>();
            _interval = TimeSpan.FromSeconds(1);
            _cts = new CancellationTokenSource();
        }

        // commends
        public void Start()
        {
            Task.Run(() => ExecuteAsync(_cts.Token));
        }
        public void Stop()
        {
            _cts.Cancel();
        }
        public void Add_ipAddress(IpInfo ipInfo)
        {
            _ipAddresses.Add(ipInfo);
        }
        public void Remove_ipAddress(IpInfo ipInfo)
        {
            _ipAddresses.Remove(ipInfo);
        }

        // async
        private async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await PingIpAddressesConcurrently(_ipAddresses, _interval);
            }
        }
        private async Task PingIpAddressesConcurrently(List<IpInfo> ipAddresses, TimeSpan interval)
        {
            var tasks = new List<Task>();
            for (int i = 0; i < ipAddresses.Count; i++)
            {
                if (ipAddresses[i].Skip <= 0)
                {
                    tasks.Add(PingHostAsync(ipAddresses[i]));

                    foreach (DisconnetedStatus level in priority)
                    {
                        if (ipAddresses[i].Disconnected <= level.dCount)
                        {
                            ipAddresses[i].Skip = level.skip;
                            break;
                        }
                    }
                }
                else
                {
                    --ipAddresses[i].Skip;
                }
            }

            await Task.WhenAll(tasks); // wait for all tasks to complete

            //Console.WriteLine("All pings sent. Waiting for the next interval...");  // debug

            await Task.Delay(interval); // wait for the specified interval
        }
        private async Task PingHostAsync(IpInfo ipInfo)
        {
            using (Ping ping = new Ping())
            {
                PingReply reply = await ping.SendPingAsync(ipInfo.Address, 1250);
                bool newStatus = reply.Status == IPStatus.Success;
                if (ipInfo.History.Count > 0)
                {
                    if (ipInfo.History.Last().Status != newStatus)
                    {
                        ipInfo.CustomUpdate(newStatus);
                    }
                }
                else
                {
                    ipInfo.CustomUpdate(newStatus);
                }
                //Console.WriteLine($"{ipInfo.Address} - {reply.Status}");  // debug
            }
        }
    }

    public class IpInfo
    {
        private string address;
        private List<HistoryStorage> historyStorage;

        private int skip;
        private int disconnected;

        private bool disconnectedMSG;
        
        // builder
        public IpInfo(string address)
        {
            this.address = address;
            historyStorage = new List<HistoryStorage>();

            skip = 0;
            disconnected = 0;
            disconnectedMSG = false;
        }

        public IpInfo(IpInfo info)
        {
            address = info.address;
            historyStorage = new List<HistoryStorage>(info.History);
            skip = info.skip;
            disconnected = info.disconnected;
            disconnectedMSG = info.disconnectedMSG;
        }

        // geters + seters
        public string Address
        {
            get {  return address; }
            set { address = value; }
        }
        public List<HistoryStorage> History
        {
            get { return historyStorage; }
            set { historyStorage = value; }
        }
        public int Skip
        {
            get { return skip; }
            set { skip = value; }
        }
        public int Disconnected
        {
            get { return disconnected; }
            //set { disconnected = value; }
        }
        public bool DisconnectedMSG
        {
            get { return disconnectedMSG; }
            set { disconnectedMSG = value; }
        }
        // others
        public void CustomUpdate(bool connected)
        {
            if (historyStorage.Count >= 50000)
            {
                historyStorage.Remove(historyStorage.First());
            }
            HistoryStorage hs = new HistoryStorage();
            hs.Status = connected;
            hs.ConnectionTime = DateTime.Now;
            historyStorage.Add(hs);

            // Disconnected
            if (connected)
            {
                disconnected = 0;
                disconnectedMSG = false;
            }
            else
            {
                if (disconnected < int.MaxValue - 10)
                {
                    ++disconnected;
                }
            }
        }
    }

    public struct DisconnetedStatus
    {
        public int level;
        public int dCount;
        public int skip;
    }

    public struct HistoryStorage
    {
        private bool status;
        private DateTime connectionTime;
        
        public bool Status
        {
            get { return status; }
            set { status = value; }
        }
        public DateTime ConnectionTime
        {
            get { return connectionTime; }
            set { connectionTime = value; }
        }

    }
}
