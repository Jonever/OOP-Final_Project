using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace Client_Forms
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!backgroundWorker1.IsBusy) {
                backgroundWorker1.RunWorkerAsync();
            }
        }
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            CheckForIllegalCrossThreadCalls = false;
            string host = textBox5.Text;
            string username = textBox1.Text;
            string passwrod = textBox2.Text;
            string quantity = textBox3.Text;
            string duration = textBox4.Text;

            byte[] bytes = new byte[1024];
            IPAddress ipAddress = IPAddress.Parse(host);
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, 11000);
            Socket socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            
            try
            {
                string sendCode = "{\"username\":\""+username+"\", \"password\":\""+passwrod+"\", \"count\": "+quantity+", \"duration\": "+duration+"}";
                socket.Connect(remoteEP);
                byte[] msg = Encoding.ASCII.GetBytes(sendCode + "<EOF>");
    
                int bytesSent = socket.Send(msg);
                int bytesRec = socket.Receive(bytes);
                string res = Encoding.ASCII.GetString(bytes, 0, bytesRec);
                
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                MessageBox.Show(res);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
           
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (!backgroundWorker2.IsBusy)
            {
                backgroundWorker2.RunWorkerAsync();
            }
        }
        public class Voucher
        {
            public string code { get; set; }
            public int duration { get; set; }
            public string date { get; set; }
        }

        private String converter(int sec) {
            TimeSpan t = TimeSpan.FromSeconds(sec);
            
            return string.Format("{0:D2}:{1:D2}:{2:D2}",
                            t.Hours,
                            t.Minutes,
                            t.Seconds);
        }
        private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            Dictionary<string, Voucher> vouchers = new Dictionary<string, Voucher>();
            CheckForIllegalCrossThreadCalls = false;
            string host = textBox5.Text;
            string username = textBox1.Text;
            string passwrod = textBox2.Text;
            
            byte[] bytes = new byte[100000];
            IPAddress ipAddress = IPAddress.Parse(host);
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, 11000);
            Socket socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                richTextBox1.Clear();
                string sendCode = "{\"username\":\"" + username + "\", \"password\":\"" + passwrod + "\", \"getVoucher\":\"Ok\"}";
                socket.Connect(remoteEP);
                byte[] msg = Encoding.ASCII.GetBytes(sendCode + "<EOF>");

                int bytesSent = socket.Send(msg);
                int bytesRec = socket.Receive(bytes);
               
                string res = Encoding.ASCII.GetString(bytes, 0, bytesRec);

                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                

                vouchers = JsonConvert.DeserializeObject<Dictionary<string, Voucher>>(res);
                int count = 0;
                foreach (string k in vouchers.Keys)
                {
                    try {
                        richTextBox1.AppendText("Code: " + vouchers[k].code + " Duration: " + converter(vouchers[k].duration) + "\r\n");
                        richTextBox1.SelectionStart = richTextBox1.TextLength;
                        richTextBox1.ScrollToCaret();
                        //richTextBox1.Refresh();
                        count++;
                    } catch { }
                    
                }
                richTextBox1.AppendText("Voucher Qty. " + count + " pcs.\r\n") ;
                richTextBox1.SelectionStart = richTextBox1.TextLength;
                richTextBox1.ScrollToCaret();
                //richTextBox1.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private bool isCountdown = false;
        private void backgroundWorker3_DoWork(object sender, DoWorkEventArgs e)
        {
            
            CheckForIllegalCrossThreadCalls = false;
            string host = textBox6.Text;
            string voucher = textBox7.Text;
            isCountdown = true;
            btnActivate.Text = "Stop";
            try
            {

                byte[] bytes = new byte[1028];
                IPAddress ipAddress = IPAddress.Parse(host);
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, 11000);
                Socket socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                string sendCode = "{\"checkVoucher\":\""+voucher+"\"}";
                socket.Connect(remoteEP);
                byte[] msg = Encoding.ASCII.GetBytes(sendCode + "<EOF>");
                int bytesSent = socket.Send(msg);
                int bytesRec = socket.Receive(bytes);
                string res = Encoding.ASCII.GetString(bytes, 0, bytesRec);
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                if (res.Contains("Not Found"))
                {
                    MessageBox.Show(res);
                }
                else {
                   Voucher v = JsonConvert.DeserializeObject<Voucher>(res);
                    if (v.duration > 0) {
                        int countdown = v.duration;
                        for (int i = countdown; i >= 0; i--) {
                            label8.Text = "Count Down: " + converter(i);
                            System.Threading.Thread.Sleep(1000);
                            if (!isCountdown) { 
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            isCountdown = false;
            btnActivate.Text = "Activate";
        }
        
        private void btnActivate_Click(object sender, EventArgs e)
        {
            if (!backgroundWorker3.IsBusy)
            {
                backgroundWorker3.RunWorkerAsync();
            }
            else { isCountdown = false;}
        }
    }
}