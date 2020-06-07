using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Net;

namespace faceRecognition
{
    /// <summary>
    /// Данный класс реализует инструменты для работы с сервером
    /// </summary>
    class WebTools : IDisposable
    {
        bool disposed = false;
        public WebClient webClient = new WebClient();
        
        /// <summary>
        /// Метод отправки на сервер изображения и JSON строки с ним 
        /// </summary>
        /// <param name="serverAddress">Адрес сервера</param>
        /// <param name="filename">Имя файла</param>
        /// <param name="serverMethod">Метод сервера</param>
        /// <param name="strToPost">JSON строка для отправки</param>
        /// <returns> Ответ от сервера </returns>
        public string postImageToServer(string serverAddress, string filename, string serverMethod, string strToPost)
        {
            
            byte[] imageByte = File.ReadAllBytes(filename);
            File.Delete(filename);
            byte[] stringToRequest = Encoding.UTF8.GetBytes(strToPost + "<image>");
            WebRequest request = WebRequest.Create(serverAddress + "/" + serverMethod);
            request.ContentLength = imageByte.Length + stringToRequest.Length;
            request.Method = "POST";
            Stream dataStream = request.GetRequestStream();
            // Write the data to the request stream.
            dataStream.Write(stringToRequest, 0, stringToRequest.Length);
            dataStream.Write(imageByte, 0, imageByte.Length);
            // Close the Stream object.
            dataStream.Close();
            // Get the response.
            WebResponse response = request.GetResponse();
            // Display the status.
            // Get the stream containing content returned by the server.
            dataStream = response.GetResponseStream();
            // Open the stream using a StreamReader for easy access.
            StreamReader reader = new StreamReader(dataStream);
            // Read the content.
            string responseFromServer = reader.ReadToEnd();
            // Display the content.
            // Clean up the streams.
            reader.Close();
            dataStream.Close();
            response.Close();
            return responseFromServer;
        }

        /// <summary>
        /// Перегрузка предыдущего метода для отправки серверу только изображения
        /// </summary>
        /// <param name="serverAddress">Адрес сервера</param>
        /// <param name="filename">Имя файла</param>
        /// <param name="serverMethod">Метод сервера</param>
        /// <returns>Возвращает ответ сервера</returns>

        public string postImageToServer(string serverAddress, string filename, string serverMethod)
        {
            byte[] imageByte = File.ReadAllBytes(filename);
           
            
            WebRequest request = WebRequest.Create(serverAddress + "/" + serverMethod);
            
            request.ContentLength = imageByte.Length;
            request.Method = "POST";
            Stream dataStream = request.GetRequestStream();
            // Write the data to the request stream.
            //dataStream.Write(stringToRequest, 0, stringToRequest.Length);
            dataStream.Write(imageByte, 0, imageByte.Length);
            // Close the Stream object.
            dataStream.Close();
            // Get the response.
            WebResponse response = request.GetResponse();
            // Display the status.
            // Get the stream containing content returned by the server.
            dataStream = response.GetResponseStream();
            // Open the stream using a StreamReader for easy access.
            StreamReader reader = new StreamReader(dataStream);
            // Read the content.
            string responseFromServer = reader.ReadToEnd();
            // Display the content.
            // Clean up the streams.
           
            reader.Close();
            response.Close();
            return responseFromServer;
        }

        /// <summary>
        /// Метод для отправки запроса функции полнотекстового поиска
        /// </summary>
        /// <param name="serverAddress">Адрес сервера</param>
        /// <param name="searchPhrase">Фраза для поиска</param>
        /// <returns>JSON строку с найденными записями</returns>

        public string fullTextSearchRequest(string serverAddress, string searchPhrase)
        {
            string requestPhrase = searchPhrase;
            WebRequest req = WebRequest.Create(serverAddress + "/full_text_search");
            req.Method = "POST";
            byte[] requestByte = Encoding.UTF8.GetBytes(requestPhrase);
            req.ContentLength = requestByte.Length;
            Stream requestStream = req.GetRequestStream();
            requestStream.Write(requestByte, 0, requestByte.Length);
            requestStream.Close();

            WebResponse res = req.GetResponse();
            requestStream = res.GetResponseStream();
            StreamReader readResponse = new StreamReader(requestStream);
            string responsedString = readResponse.ReadToEnd();
            readResponse.Close();
            res.Close();
            return responsedString;
        }

        /// <summary>
        /// Метод для получения количества пользователей в БД
        /// </summary>
        /// <param name="serverAddress">Адрес сервера</param>
        /// <returns>Количество пользователей</returns>

        public string getUserCountFromServer(string serverAddress)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create("http://localhost:1111/get_users_count");
            HttpWebResponse response = null;
            response = (HttpWebResponse)req.GetResponse();
            Stream responseData = response.GetResponseStream();
            StreamReader readResponse = new StreamReader(responseData);
            string getRequestData = readResponse.ReadToEnd();
            response.Close();
            responseData.Close();
            readResponse.Close();
            return getRequestData;
        }

        /// <summary>
        /// Метод реализующий отправку запроса на удаление записи по id
        /// </summary>
        /// <param name="serverAddress">Адресс сервера</param>
        /// <param name="id">id пользователя</param>
        /// <returns>Ответ сервера</returns>

        public string deleteRecordRequest(string serverAddress, int id)
        {
            WebRequest req = WebRequest.Create(serverAddress + "/delete_user_record");
            req.Method = "POST";
            byte[] requestByte = Encoding.UTF8.GetBytes(id.ToString());
            req.ContentLength = requestByte.Length;
            Stream requestStream = req.GetRequestStream();
            requestStream.Write(requestByte, 0, requestByte.Length);
            requestStream.Close();

            WebResponse res = req.GetResponse();
            requestStream = res.GetResponseStream();
            StreamReader readResponse = new StreamReader(requestStream);
            string response = readResponse.ReadToEnd();
            readResponse.Close();
            requestStream.Close();
            res.Close();
            return response;
        }

        /// <summary>
        /// Метод для отправки серверу запроса на обновление записи пользователя
        /// </summary>
        /// <param name="serverAddress">Адрес сервера</param>
        /// <param name="id">id в БД</param>
        /// <param name="updateString">Строка для обновления</param>
        /// <returns>Ответ сервера</returns>

        public string updateRecordRequest(string serverAddress, int id, string updateString)
        {
            WebRequest req = WebRequest.Create(serverAddress + "/update_user_record");
            req.Method = "POST";
            byte[] requestByte = Encoding.UTF8.GetBytes(updateString);
            req.ContentLength = requestByte.Length;
            Stream requestStream = req.GetRequestStream();
            requestStream.Write(requestByte, 0, requestByte.Length);
            requestStream.Close();

            WebResponse res = req.GetResponse();
            requestStream = res.GetResponseStream();
            StreamReader readResponse = new StreamReader(requestStream);
            string response = readResponse.ReadToEnd();
            readResponse.Close();
            requestStream.Close();
            res.Close();
            return response;
        }

        /// <summary>
        /// Метод для загрузки изображений с сервера
        /// </summary>
        /// <param name="filename">Имя файла на сервере</param>
        /// <param name="serverAddress">Адрес сервера</param>

        public void downloadFileFromServer(string filename, string serverAddress)
        {
            webClient.DownloadFile(serverAddress + "/download_image/images/" + filename, "images\\" + filename);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            disposed = true;
        }
        

    }
}
