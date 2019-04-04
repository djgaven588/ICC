using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using System.Windows.Threading;
using ChatApplication;

namespace WPF_Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Queue<string> messagesToSend = new Queue<string>();
        private Client client;
        public MainWindow()
        {
            InitializeComponent();
            client = new Client(this.MessageHistory);

            DispatcherTimer timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(0.1)
            };
            timer.Tick += (o, e) =>
            {
                DateTime dtCurrentTime = DateTime.Now;
                this.ConnectionStatus.Content = client.Loop(messagesToSend);
                messagesToSend.Clear();
            };
            timer.IsEnabled = true;
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            //Send Message
            messagesToSend.Enqueue(this.MessageToSend.Text);
            this.MessageToSend.Text = "";
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            //Connect
            client.Connect(this.ServerAddress.Text, int.Parse(this.ServerPort.Text));
        }
    }
}
