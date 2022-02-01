using System;
using System.Threading;
using System.Net.Sockets;
using System.Text;

using libHeader;
using Core_TSP_Chat_0_Client;

namespace ChatClient
{
    class Program
    {
        
        

        static void Main(string[] args)
        {

            ClientClass clientClass = new ClientClass();

            try
            {
                clientClass.StartClient();

                // запускаем новый поток для получения данных
                Thread receiveThread = new Thread(new ThreadStart(clientClass.ReceiveMessage));
                receiveThread.Start(); //старт потока

                clientClass.SendMessage();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                clientClass.Disconnect();
            }
        }




        



    }



}