using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace faceRecognition.Exceptions
{
    public class AuthException : Exception
    {
        public AuthException(string message, int code) : base ($"Ошибка при получении токена авторизации: {message}, HTTP-код: {code}") { }
    }
}
