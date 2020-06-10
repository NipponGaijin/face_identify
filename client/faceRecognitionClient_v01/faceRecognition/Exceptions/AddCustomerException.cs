using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace faceRecognition.Exceptions
{
    public class AddCustomerException : Exception
    {
        public AddCustomerException(string message, int code) : base($"Ошибка при добавлении нового посетителя: {message}, HTTP-код: {code}") { }

        public AddCustomerException(string message) : base($"Ошибка при добавлении нового посетителя: {message}") { }
    }
}
