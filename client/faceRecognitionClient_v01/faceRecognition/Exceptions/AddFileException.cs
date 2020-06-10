using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace faceRecognition.Exceptions
{
    public class AddFileException : Exception
    {
        public AddFileException(string message, int code) : base($"Ошибка при идентификации посетителя: {message}, HTTP-код: {code}") { }

        public AddFileException(string message) : base($"Ошибка при идентификации посетителя: {message}") { }
    }
}
