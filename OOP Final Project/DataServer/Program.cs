using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace DataServer
{
    class Program
    {
        public class StateObject
        {

            public Socket workSocket = null;
            public const int BufferSize = 1024;
            public byte[] buffer = new byte[BufferSize];
            public StringBuilder sb = new StringBuilder();
        }
        public class AsynchronousSocketListener
        {
            public static ManualResetEvent allDone = new ManualResetEvent(false);
            public static Tools genRandom = new Tools();
            public AsynchronousSocketListener()
            {

            }

            public static void StartListening()
            {
                IPHostEntry iplist = Dns.GetHostEntry(Dns.GetHostName());
                IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
                Console.Write("Server IP: ");
                foreach (IPAddress ip in iplist.AddressList)
                {
                    if (ip.AddressFamily.Equals(AddressFamily.InterNetwork))
                    {
                        Console.WriteLine(ip);
                        ipAddress = ip;
                        break;
                    }
                }


                IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);
                Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                try
                {
                    listener.Bind(localEndPoint);
                    listener.Listen(100);
                    Console.WriteLine("Server Start");
                    Console.WriteLine();
                    while (true)
                    {
                        allDone.Reset();
                        listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
                        allDone.WaitOne();
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }

                Console.WriteLine("\nPress ENTER to continue...");
                Console.Read();

            }

            public static void AcceptCallback(IAsyncResult ar)
            {
                allDone.Set();
                Socket listener = (Socket)ar.AsyncState;
                Socket handler = listener.EndAccept(ar);
                StateObject state = new StateObject();
                state.workSocket = handler;
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
            }

            public static void ReadCallback(IAsyncResult ar)
            {
                String content = String.Empty;

                StateObject state = (StateObject)ar.AsyncState;
                Socket handler = state.workSocket;
                int bytesRead = handler.EndReceive(ar);

                if (bytesRead > 0)
                {
                    state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));
                    content = state.sb.ToString();
                    if (content.IndexOf("<EOF>") > -1)
                    {
                        content = content.Replace("<EOF>", string.Empty);
                        try
                        {

                            Generate_voucher json = JsonConvert.DeserializeObject<Generate_voucher>(content);
                            if (json != null)
                            {
                                if (json.getVoucher != null)
                                {
                                    
                                    string dataString = JsonConvert.SerializeObject(Tools.vouchers);
                                   
                                    Send(handler, dataString);
                                }
                                else if (json.username != null && json.password != null)
                                {
                                    string username = json.username;
                                    string password = json.password;
                                    int count = json.count;
                                    if (username.Equals("admin") && password.Equals("admin"))
                                    {
                                        if (json.count > 0)
                                        {
                                            Console.WriteLine("Generating...");
                                            for (int i = 0; i < json.count; i++)
                                            {
                                                String random = genRandom.RandomString(6);
                                                Voucher v = new Voucher();
                                                v.code = random;
                                                v.duration = json.duration;
                                                v.date = DateTime.Now.ToString();
                                                Tools.vouchers.Add(random, v);
                                                Console.WriteLine("Voucher: " + v.code + " Duration: " + v.duration + " Date" + v.date);
                                            }
                                            Console.WriteLine("Generating Code Count: " + json.count + " Successs..");
                                            Console.WriteLine("Total Data Item: " + Tools.vouchers.Count);
                                            Send(handler, "Success Generating Voucher");
                                            string dataString = JsonConvert.SerializeObject(Tools.vouchers);
                                            Tools.writeFile(dataString);
                                        }
                                        else
                                        {
                                            Send(handler, "Generat voucher count must be not less than 1");
                                        }
                                    }
                                    else
                                    {
                                        Send(handler, "Invalid Username or Password");
                                    }
                                }
                                else if (json.checkVoucher != null)
                                {
                                    try
                                    {
                                        Voucher v = Tools.vouchers[json.checkVoucher];
                                        Tools.vouchers.Remove(json.checkVoucher);
                                        string data = JsonConvert.SerializeObject(Tools.vouchers);
                                        Tools.writeFile(data);
                                        string dataString = JsonConvert.SerializeObject(v);
                                        Send(handler, dataString);
                                    }
                                    catch
                                    {
                                        Send(handler, "Voucher Not Found.");
                                    }

                                }
                            }

                        }
                        catch (Exception ex)
                        {
                            Send(handler, "Invalid Quiry");

                            Console.WriteLine(ex.ToString());
                        }
                    }
                    else
                    {
                        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReadCallback), state);
                    }
                }
            }

            private static void Send(Socket handler, String data)
            {
                byte[] byteData = Encoding.ASCII.GetBytes(data);
                handler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), handler);
            }

            private static void SendCallback(IAsyncResult ar)
            {
                try
                {
                    Socket handler = (Socket)ar.AsyncState;
                    handler.SendTimeout = 500;
                    int bytesSent = handler.EndSend(ar);
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }

            public static int Main(String[] args)
            {
                Tools.readFile();
                StartListening();
                return 0;
            }
        }
    }

    public class Voucher
    {
        public string code { get; set; }
        public int duration { get; set; }
        public string date { get; set; }
    }
    public class Generate_voucher
    {
        public string username { get; set; }
        public string password { get; set; }
        public int count { get; set; }
        public int duration { get; set; }
        public string getVoucher { get; set; }
        public string checkVoucher { get; set; }
    }
    public class Tools
    {
        Random random;
        public static Dictionary<string, Voucher> vouchers = new Dictionary<string, Voucher>();
        public Tools()
        {
            random = new Random();
        }
        public string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }
        public static void writeFile(String str)
        {
            try
            {
                StreamWriter sw = new StreamWriter("./data.txt");
                sw.WriteLine(str);
                sw.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.Message);
            }
            finally
            {
                Console.WriteLine("Data Saved");
            }
        }
        public static void readFile()
        {
            String line = "{}";
            try
            {
                line = File.ReadAllText("./data.txt");
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.Message);
            }
            finally
            {
                vouchers = JsonConvert.DeserializeObject<Dictionary<string, Voucher>>(line);
                
            }
        }
    }
}