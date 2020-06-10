using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace faceRecognition.Exceptions
{
    public class IdentifyException : Exception
    {
        public IdentifyException(string message, int code) : base($"Ошибка при идентификации посетителя: {message}, HTTP-код: {code}") { }

        public IdentifyException(string message) : base($"Ошибка при идентификации посетителя: {message}") { }
    }
}
