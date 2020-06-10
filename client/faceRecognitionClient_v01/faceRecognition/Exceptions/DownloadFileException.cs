using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace faceRecognition.Exceptions
{
    public class DownloadFileException : Exception
    {
        public DownloadFileException(string message, int code) : base($"Ошибка при загрузке файла с сервера: {message}, HTTP-код: {code}") { }
    }
}
