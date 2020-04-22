using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net.Mail;
using System.Xml.Schema;

namespace WpfApp1
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        Socket ListenSocket;
        Boolean Listening;
        public MainWindow()
        {
            InitializeComponent();
        }


        private void Sender_Click(object sender, RoutedEventArgs e)
        {
            string ip = IpTextBox.Text; string port = PortTextBox.Text;
            string content = OutPutTextBox.Text;
            Task.Run(() =>
            {
                try
                {
                    Sender sender1 = new Sender();
                    sender1.Connect(ip, port);
                    sender1.Send(content);
                    this.Dispatcher.Invoke(() => StateTextBox.Text = "发送成功");
                }
                catch (Exception exc)
                {
                    this.Dispatcher.Invoke(() => StateTextBox.Text = exc.Message);
                }
            }
            );
        }

        private void Listen_Click(object sender, RoutedEventArgs e)//解决了//现有bug：1.关闭监听后无法重新开启，因为socket已经被释放。2.监听后会卡。
        {
            if (ListenSocket != null) { StateTextBox.Text = "已经开始监听了，如果想更换端口，请先停止监听"; return; }
            ListenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            string ip = ListenIp.Text; string port = ListenPort.Text;

            Task.Run(() =>
              {
                  try
                  {
                      if (!ListenSocket.IsBound)
                      {
                          Receiver receiver = new Receiver();
                          receiver.Bind(ip, port, ref ListenSocket);
                          ListenSocket.Listen(10);
                          Listening = true;
                          this.Dispatcher.Invoke(() => StateTextBox.Text = "监听成功");
                          this.Dispatcher.Invoke(() => LEDController(Colors.Green));

                          while (Listening)//因为这里面的invoke是同步的，但是showMessage里的Accept()是阻塞的，所以会卡。
                          {
                              receiver.BlockedAccept(ref ListenSocket);
                              this.Dispatcher.Invoke(() => InPutTextBox.Text = receiver.ShowMessage());
                          }
                      }
                  }
                  catch (Exception exc)
                  {
                      this.Dispatcher.Invoke(() => StateTextBox.Text = exc.Message);
                  }

              });

        }

        private void Break_Click(object sender, RoutedEventArgs e)
        {
            if (ListenSocket == null) { StateTextBox.Text = "没有在监听";return; }
            Listening = false;
            ListenSocket.Close();
            ListenSocket = null;
            StateTextBox.Text = "结束监听";
            LEDController(Colors.Red);
        }
        private void LEDController(Color color)
        {
            var brush = new SolidColorBrush();
            brush.Color = color;
            LED.Fill = brush;
        }
    }
    internal class Sender
    {
        IPAddress ip;
        IPEndPoint iPEndPoint;
        Socket s;
        Byte[] buffer;
        internal Sender()
        {
            s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }
        internal void Connect(string ipString, string port)
        {
            ip = IPAddress.Parse(ipString);
            iPEndPoint = new IPEndPoint(ip, Convert.ToInt32(port));
            s.Connect(iPEndPoint);
        }
        internal void Send(string content)
        {
            buffer = Encoding.UTF8.GetBytes(content);
            s.Send(buffer, buffer.Length, 0);
            s.Close();
        }
    }

    internal class Receiver
    {
        IPAddress _ip;
        IPEndPoint _IPEndPoint;
        Byte[] recByte = new byte[1024];
        int byteSize;
        Socket temp;

        internal void Bind(string ipString, string port, ref Socket ListenSocket)
        {
            _ip = IPAddress.Parse(ipString);
            _IPEndPoint = new IPEndPoint(_ip, Convert.ToInt32(port));
            ListenSocket.Bind(_IPEndPoint);
        }
        internal void BlockedAccept(ref Socket ListenSocket)
        {
            temp = ListenSocket.Accept();
        }
        internal string ShowMessage()
        {
            byteSize = temp.Receive(recByte, recByte.Length, 0);
            byte[] GoodByte = new byte[byteSize];
            Array.Copy(recByte, GoodByte, byteSize);
            Array.Clear(recByte, 0, recByte.Length);
            string content = Encoding.UTF8.GetString(GoodByte);
            temp.Close();
            return content;
        }
    }
}
