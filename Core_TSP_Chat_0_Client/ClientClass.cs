using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;


using libHeader;
using System.IO;

namespace Core_TSP_Chat_0_Client
{
    class ClientClass
    {
        const int TRANSFER_SIZE = 10485760; //10 Mb

        public string UserName {private set; get; }
        private const string host = "127.0.0.1";
        private const int port = 8888;
        private TcpClient client;
        private NetworkStream stream;

        HeaderClass headerClient = null;
        HeaderClass headerServer = null;

        public void StartClient()
        {

            headerClient = new HeaderClass(); // отправляемый header
            headerServer = new HeaderClass(); //принимаемый от сервера header

            Console.Write("Введите свое имя: ");
            UserName = Console.ReadLine();

            headerClient.userName = UserName;

            client = new TcpClient();
            try
            {
                client.Connect(host, port); //подключение клиента
                stream = client.GetStream(); // получаем поток

                //string message = userName;
                //byte[] data = Encoding.Unicode.GetBytes(message);
                //stream.Write(data, 0, data.Length);

                // запускаем новый поток для получения данных
                Thread receiveThread = new Thread(new ThreadStart(ReceiveMessage));
                receiveThread.Start(); //старт потока
                Console.WriteLine("Добро пожаловать, {0}", UserName);
                SendMessage();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Disconnect();
            }
        }




        // отправка сообщений
        public void SendMessage()
        {
            while (true)
            {

                bool nextStep = false; //обнуляем переменную

                string switchString = "";
                while (!nextStep)
                {
                    Console.WriteLine("Выберите тип сообщения:\n 1) Сообщение\n 2) Файл \n 3) Отключиться от сервера\n");
                    switchString = Console.ReadLine();

                    if (switchString == "" || (switchString != "1" && switchString != "2" && switchString != "3"))
                    {
                        Console.WriteLine("Ошибка. Ничего не было выбрано.\n");
                        nextStep = false;
                        continue;
                    }
                    nextStep = true;
                }

                switch (switchString)
                {
                    case "1":
                        {
                            string payloadString = "";
                            Console.WriteLine("Введите сообщение:");
                            payloadString = Console.ReadLine();

                            if (payloadString == "") payloadString = " "; //полезная нагрузка не может быть 0 byte

                            //заполняем header
                            headerClient.startWord = HeaderClass.START_WORD;
                            headerClient.typeMessage = TypeMessage.Message; //тип полезной нагрузки - "Сообщение"                           


                            headerClient.payloadSize = Encoding.UTF8.GetBytes(payloadString).Length; //длинна Сообщения в байтах                           


                            //отправляем сообщение на сервер
                            SendStringMessage(headerClient, payloadString);

                            break;
                        }
                    case "2": 
                        {
                            //отправка файла                         

                            Console.WriteLine("Вставьте путь до файла.");
                            string payload_path = Console.ReadLine();
                            Console.WriteLine("Путь: " + payload_path);

                            FileInfo infoFile = new FileInfo(payload_path);
                            //Существует ли файл?
                            if (!infoFile.Exists)
                            {
                                Console.WriteLine("Ошибка. Такого файла не сушествует.");
                                Console.WriteLine();                                
                                break;
                            }

                            //заполняем header
                            headerClient.startWord = HeaderClass.START_WORD;
                            headerClient.typeMessage = TypeMessage.File; //тип полезной нагрузки - "Файл" 
                            headerClient.fileName = infoFile.Name;
                            headerClient.payloadSize = infoFile.Length;

                            //создаем обработчик ответа передачи файла (класс FileHeader)
                            FileHeader fileHeader = null;

                            //Отправили [HeaderSize] + [Header] + [File]
                            //получили  [FileHeader] c ответом от получателя
                            //парсим [FileHeader] получил ли он файлы.
                            //Если получил - продолжать передачу, если есть еще части файла
                            //Если не получил - попробовать передать снова

                            if (infoFile.Length > TRANSFER_SIZE)
                            {
                                //достать из файла 10Мб
                                //отправить
                                //дождаться ответа с [FileHeader]
                                //десериализировать [FileHeader]
                                //вычислить CRC32 переданного пакета
                                //сравнить CRC32 и данные из [FileHeader]
                                //если все ок - продолжаем отправку следующих 10мб






                            }
                            else
                            {



                            }

                            



                            break; 
                        }
                    case "3": { break; }

                }


            }          


       
        }



        // получение сообщений
        public void ReceiveMessage()
        {
            while (true)
            {
                try
                {
                    byte[] data = new byte[64]; // буфер для получаемых данных
                    StringBuilder builder = new StringBuilder();
                    int bytes = 0;
                    do
                    {
                        bytes = stream.Read(data, 0, data.Length);
                        builder.Append(Encoding.UTF8.GetString(data, 0, bytes));
                    }
                    while (stream.DataAvailable);

                    string message = builder.ToString();
                    Console.WriteLine(message);//вывод сообщения
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine("Подключение прервано!"); //соединение было прервано
                    Console.ReadLine();
                    Disconnect();
                }
            }
        }


        /// <summary>
        /// Отправляет header и messagePayload на сервер
        /// </summary>
        /// <param name="headerToSend"></param>
        /// <param name="message"></param>
        private void SendStringMessage(HeaderClass headerToSend, string message)
        {
            if (message.Length <= 0)
            {
                Console.WriteLine("[ERROR] message.Length <= 0");
                return;
            }
            if (headerToSend == null)
            {
                Console.WriteLine("[ERROR] headerToSend == null");
                return;
            }          

            
            //десериализуем header
            string headerString = JsonSerializer.Serialize<HeaderClass>(headerToSend);

            //заполняем раздел [headerSize]
            byte[] headerSize = new byte[4];// создаем массив для хранения размера header в байтах
            int tmp_h_size = headerString.Length; // получаем длинну header в байтах                            
            headerSize[0] = (byte)(tmp_h_size >> 24); // побайтово заполняем массив
            headerSize[1] = (byte)(tmp_h_size >> 16);
            headerSize[2] = (byte)(tmp_h_size >> 8);
            headerSize[3] = (byte)(tmp_h_size & 0xFF);

            //вывод на консоль
            Console.WriteLine($"HeaderString: {headerString}");
            Console.WriteLine($"[headerSize]: {tmp_h_size} byte - [0]: {headerSize[0]} | [1] {headerSize[1]} | [2] {headerSize[2]} | [3] {headerSize[3]}"); //вывод на консоль [headerSize]
            PrintHeader(headerToSend);

            //заполняем [Header] буфер
            byte[] headerSection = new byte[headerString.Length];
            headerSection = Encoding.UTF8.GetBytes(headerString);

            //заполняем буфер [payload]
            byte[] payloadMessageBuff = new byte[message.Length];
            payloadMessageBuff = Encoding.UTF8.GetBytes(message);

            byte[] total = new byte[headerSize.Length + headerSection.Length + payloadMessageBuff.Length];

            //копируем все разделы в буфер total = [startWord 4 byte][headerSize] + [header] + [payload]
            Buffer.BlockCopy(headerSize, 0, total, 0, headerSize.Length);
            Buffer.BlockCopy(headerSection, 0, total, headerSize.Length, headerSection.Length);
            Buffer.BlockCopy(payloadMessageBuff, 0, total, headerSize.Length + headerSection.Length, payloadMessageBuff.Length);

            //отправляем все байты
            stream.Write(total, 0, total.Length);
        }




        private void SendFile(HeaderClass headerToSend, string payload_path)
        {

            FileInfo infoFile = new FileInfo(payload_path);
            //Существует ли файл?
            if (!infoFile.Exists)
            {
                Console.WriteLine("Ошибка. Такого файла не сушествует.");
                Console.WriteLine();
                return;
            }

            //заполняем header
            headerToSend.startWord = HeaderClass.START_WORD;
            headerToSend.typeMessage = TypeMessage.File; //тип полезной нагрузки - "Файл" 
            headerToSend.fileName = infoFile.Name;
            headerToSend.payloadSize = infoFile.Length;


            //читаем файл
            using (FileStream fstream = File.OpenRead(payload_path))
            {
                byte[] array;
                //long positionInFile = 0; // счетчик позиции в файле. Если размер больше 1МБ то будем яитать по 1МБ и перемещать счетчик на 1МБ
                long fileSize = infoFile.Length;


                // считываем данные                                  

                Console.WriteLine($"Передаем файл: {infoFile.Name}");
                bool nStep = false;
                int k = 0;
                do //считывать файл и передавать, пока не закончатся байты файла
                {


                    if (fileSize > TRANSFER_SIZE)
                    {
                        Console.WriteLine($"{k} Размер файла: {infoFile.Length} | Осталось данных: {fileSize}");
                        array = new byte[TRANSFER_SIZE];
                        fstream.Read(array, 0, TRANSFER_SIZE); // читаем в буфер из файла 1МБ
                        stream.Write(array, 0, TRANSFER_SIZE); //передаем на сервер
                        fileSize -= TRANSFER_SIZE; // вычитаем из общего размера файла то, что уже передали

                        //принимаем от получателя [FileHeader]



                        //десериализируем [FileHeader]

                        //вычисляем CRC32
                        int crc32_value = CRC32_calc(array, TRANSFER_SIZE);

                        //сравнимаем CRC32



                        array = null;
                        nStep = false;
                    }
                    else
                    {
                        Console.WriteLine($"{k} Размер файла: {infoFile.Length} | Осталось данных: {fileSize}");
                        array = new byte[fileSize];
                        fstream.Read(array, 0, (int)fileSize); // читаем в буфер из файла 
                        stream.Write(array, 0, (int)fileSize); //передаем на сервер

                        nStep = true;
                    }

                    k++;
                } while (!nStep);

                Console.WriteLine("Файл отправлен.");
            }


        }






        private void PrintHeader(HeaderClass header)
        {
            Console.WriteLine($"header.startWord: {header.startWord}");
            Console.WriteLine($"header.typeMessage: {header.typeMessage.ToString()}");
            Console.WriteLine($"header.payloadSize: {header.payloadSize} byte");
            Console.WriteLine($"header.fileName: {header.fileName}");
            Console.WriteLine($"header.userName: {header.userName}");
        }



        public int CRC32_calc(byte[] data, int sizeData)
        {
            long total_bytes = 0;
            for (int i = 0; i < sizeData; i++)
            {
                total_bytes += data[i];
            }
            int rezult = (int)(total_bytes & 0xFFFFFFFF);
            return rezult;
        }




        public void Disconnect()
        {
            if (stream != null)
                stream.Close();//отключение потока
            if (client != null)
                client.Close();//отключение клиента
            Environment.Exit(0); //завершение процесса
        }




        public FileHeader ReceiveFileHeader()
        {
            byte[] data = new byte[64]; // буфер для получаемых данных
            StringBuilder builder = new StringBuilder();
            int bytes = 0;
            do
            {
                bytes = stream.Read(data, 0, data.Length);
                builder.Append(Encoding.UTF8.GetString(data, 0, bytes));
            }
            while (stream.DataAvailable);

            string message = builder.ToString();

            string headerString = JsonSerializer.Serialize<HeaderClass>(headerToSend);

            return null;
        }



    }




}

