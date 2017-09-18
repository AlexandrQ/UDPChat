using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace UDPChat
{
    public partial class Form1 : Form
    {   
        const int LOCALPORT = 8001;         // порт для приема сообщений
        const int REMOTEPORT = 8001;        // порт для отправки сообщений
        const int TTL = 20;
        const string HOST = "235.5.5.1";    // хост для групповой рассылки
        IPAddress groupAddress;             // адрес для групповой рассылки
        UdpClient client;
        bool alive = false;                 // будет ли работать поток для приема
        string userName;                    // имя пользователя в чате


        public Form1()
        {
            InitializeComponent();

            loginButton.Enabled = true; // кнопка входа
            logoutButton.Enabled = false; // кнопка выхода
            sendButton.Enabled = false; // кнопка отправки
            chatTextBox.ReadOnly = true; // поле для сообщений

            groupAddress = IPAddress.Parse(HOST);
        }

        //нажатие кнопки loginButton
        private void loginButton_Click(object sender, EventArgs e)
        {
            userName = nickNameTextBox.Text;
            nickNameTextBox.ReadOnly = true;

            try
            {
                client = new UdpClient(LOCALPORT);

                //присоединяемся к групповой рассылке
                client.JoinMulticastGroup(groupAddress, TTL);

                //запускаем задачу на прием сообщений
                Task recieveTask = new Task(ReceiveMessages);
                recieveTask.Start();

                //отправляем первое сообщение о входе нового пользователя
                string message = userName + " вошел в чат";
                byte[] data = Encoding.Unicode.GetBytes(message);
                client.Send(data, data.Length, HOST, REMOTEPORT);

                loginButton.Enabled = false;
                logoutButton.Enabled = true;
                sendButton.Enabled = true;

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }



        //метод приема сообщений
        private void ReceiveMessages()
        {
            alive = true;
            try
            {
                while (alive)
                {
                    IPEndPoint remoteIP = null;
                    byte[] data = client.Receive(ref remoteIP);
                    string message = Encoding.Unicode.GetString(data);

                    //добавляем полученное сообщение в текстовое поле
                    this.Invoke(new MethodInvoker(() =>
                    {
                        string time = DateTime.Now.ToShortTimeString();
                        chatTextBox.Text = time + " " + message + "\r\n" + chatTextBox.Text;
                    }));
                }
            }
            catch (ObjectDisposedException)
            {
                if (!alive)
                    return;
                throw;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        private void sendButton_Click(object sender, EventArgs e)
        {
            try
            {
                string message = String.Format("{0}: {1}", userName, messageTextBox.Text);
                byte[] data = Encoding.Unicode.GetBytes(message);
                client.Send(data, data.Length, HOST, REMOTEPORT);
                messageTextBox.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void logoutButton_Click(object sender, EventArgs e)
        {
            ExitChat();
        }


        private void ExitChat()
        {
            string message = userName + " покидает чат";
            byte[] data = Encoding.Unicode.GetBytes(message);
            client.Send(data, data.Length, HOST, REMOTEPORT);
            client.DropMulticastGroup(groupAddress);

            alive = false;
            client.Close();

            loginButton.Enabled = true;
            logoutButton.Enabled = false;
            sendButton.Enabled = false;
        }

        // обработчик события закрытия формы
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (alive)
                ExitChat();
        }
    }
}
