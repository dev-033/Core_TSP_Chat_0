using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using libHeader;


namespace ChatServer
{
    public class ClientObject
    {
        protected internal string Id { get; private set; }
        protected internal NetworkStream Stream { get; private set; }
        public string UserName { get; set; }
        TcpClient client;
        ServerObject server; // объект сервера
        protected HeaderClass headerSection;


        public ClientObject(TcpClient tcpClient, ServerObject serverObject)
        {
            Id = Guid.NewGuid().ToString();
            client = tcpClient;
            server = serverObject;
            serverObject.AddConnection(this);
        }

        public void Process()
        {
            try
            {
                Stream = client.GetStream();

                string message;
                HeaderClass headerToReceive = null;

                // в бесконечном цикле получаем сообщения от клиента
                while (true)
                {
                    try
                    {                       
                        headerToReceive = GetHeader();

                        PrintHeader(headerToReceive);

                        if (headerToReceive.typeMessage == TypeMessage.Message)
                        {
                            //Прием сообщения
                            message = GetStringMessage(headerToReceive);
                            if (message.Length == 0)
                            {
                                message = "[ERROR MESSAGE]";
                            }
                            message = String.Format("{0}: {1}", headerToReceive.userName, message);
                            Console.WriteLine(message);
                            server.BroadcastMessage(message, this.Id);
                        }else if (headerToReceive.typeMessage == TypeMessage.File)
                        {
                            //Прием файла
                            if (String.Equals(headerToReceive.fileName, "") || headerToReceive.payloadSize == 0)
                            {
                                Console.WriteLine("[ERROR] Файл не может быть принят. Имя или размер файла равны 0");
                                continue;
                            }




                        }
                        else
                        {
                            Console.WriteLine("[ERROR] header TypeMessage unknown! ");
                        }
                        

                        

                                            

                        
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        message = String.Format("{0}: покинул чат", UserName);
                        Console.WriteLine(message);
                        server.BroadcastMessage(message, this.Id);
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                // в случае выхода из цикла закрываем ресурсы
                server.RemoveConnection(this.Id);
                Close();
            }
        }


        // чтение входящего сообщения и преобразование в строку
        //private string GetMessage()
        //{
        //    byte[] data = new byte[64]; // буфер для получаемых данных
        //    StringBuilder builder = new StringBuilder();
        //    int bytes = 0;
        //    do
        //    {
        //        bytes = Stream.Read(data, 0, data.Length);
        //        builder.Append(Encoding.UTF8.GetString(data, 0, bytes));
        //    }
        //    while (Stream.DataAvailable);

        //    return builder.ToString();
        //}

        // закрытие подключения
        protected internal void Close()
        {
            if (Stream != null)
                Stream.Close();
            if (client != null)
                client.Close();
        }


        /// <summary>
        /// Получить и десериализовывать Header
        /// 
        /// </summary>
        protected HeaderClass GetHeader()
        {
            HeaderClass header = null;          

            // получить HeaderSize
            byte[] headerSizeBuff = new byte[4]; //массив куда помещаются байты [headerSize] //// int32 это 4 байта   
            int headerSize = 0; //количество байт в разделе [header]

            //принимаем раздел [headerSize]
            int size_data = 0;
            do {
                size_data = Stream.Read(headerSizeBuff, 0, headerSizeBuff.Length);// читаем HeaderSize  [headerSize]      //// int32 это 4 байта        
            } while (Stream.DataAvailable && (size_data < 4));

            //парсим раздел [headerSize]
            headerSize = (headerSizeBuff[0] << 24) | (headerSizeBuff[1] << 16) | (headerSizeBuff[2] << 8) | (headerSizeBuff[3]); // собираем int32 из байтов //сдесь хранится [headerSize]
            Console.WriteLine("[headerSize]: " + headerSize + " байт"); //вывод в консоль           


            if (headerSize <= 0)
            {               
                Console.WriteLine("[ERROR] headerSize <= 0");                
                return null;
            }

            // Принять [Header]
            byte[] headerBuff = new byte[headerSize];
            size_data = 0;
            size_data = Stream.Read(headerBuff, 0, headerBuff.Length);// читаем [Header]

            if (size_data != headerBuff.Length)
            {               
                Console.WriteLine("[ERROR] size_data != headerBuff.Length");
                return null;
            }

            //Десериализуем Header
            StringBuilder builder = new StringBuilder();
            char[] hederStringChar = Encoding.UTF8.GetChars(headerBuff);
            foreach (var item in hederStringChar)
            {
                builder.Append(item);
            }
            string hederString = builder.ToString();
            header = JsonSerializer.Deserialize<HeaderClass>(hederString); //Десериализуем [Header]            


            //если header валидный - возвращаем header
            if (String.Equals(header.startWord, HeaderClass.START_WORD) ) // если старотовое слово == "Start"  то [Header] валидный 
            {
                return header;
            }
            else
            {
                Console.WriteLine("header.startWord [ERROR]");
                return null;
            }          
        }

        
        private string GetStringMessage(HeaderClass header)
        {
            if (header == null)
            {
                Console.WriteLine("[Error] GetStringMessage: header is null!");
                return "";
            }


            byte[] data = new byte[header.payloadSize];
            int bytes = 0;

            try
            {
                do
                {
                    bytes = Stream.Read(data, 0, data.Length);
                } while (Stream.DataAvailable && (bytes < data.Length));
                
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            if (bytes != data.Length)
            {
                Console.WriteLine("[ERROR] GetStringMessage:  bytes != data.Length");
                return "";
            }


            StringBuilder builder = new StringBuilder();
            string message = "";           
            builder.Append(Encoding.UTF8.GetString(data, 0, bytes));

            message = builder.ToString();
            return message;
        }


        private void GetFile(HeaderClass header)
        {
            //TODO



        }




        private void PrintHeader(HeaderClass header)
        {
            Console.WriteLine($"header.startWord: {header.startWord}");
            Console.WriteLine($"header.typeMessage: {header.typeMessage.ToString()}");
            Console.WriteLine($"header.payloadSize: {header.payloadSize} byte");
            Console.WriteLine($"header.fileName: {header.fileName}");
            Console.WriteLine($"header.userName: {header.userName}");
        }

    }
}
