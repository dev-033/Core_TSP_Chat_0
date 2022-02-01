using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace libHeader
{


    public enum TypeMessage
    {
        Message = 0, // текстовое сообщение
        File //файл любого типа
    }



    public class HeaderClass
    {
        [JsonIgnore]
        public const string START_WORD = "Start";

        public string startWord { set; get; } // Стартовое слово. Для проверки корректности Headera
        public string userName { set; get; } //имя отправителя
        public TypeMessage typeMessage { set; get; } //тип сообщения (полезная нагрузка) 
        public string fileName { set; get; } // имя файла который передаем.                  
        public long payloadSize { set; get; } //размер в байтах файла или сообщения  

    }





    enum TransferStatus {
        OK = 1,
        Error
    }
    public class FileHeader
    {
        public string transferStatus { get; set; }
        public int CRC32; // складываем все байты и последние 32 байта сохраняем в эту переменную
    }



   

}

