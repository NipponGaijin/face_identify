using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Net;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Net.Http;
using faceRecognition.Exceptions;

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
        /// <param name="serverAddress">Адресс сервера</param>
        /// <param name="token">Токен авторизации</param>
        /// <param name="filename">Имя пользователя</param>
        /// <param name="strToPost">Контент для загрузки</param>
        /// <returns></returns>
        public JObject AddCustomer(string serverAddress, string token, string filename, string strToPost)
        {

            UriBuilder serverUriBuilder = new UriBuilder(serverAddress);
            serverUriBuilder.Path = "/add_client";

            byte[] imageByte = File.ReadAllBytes(filename);
            File.Delete(filename);
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                using (var content = new MultipartFormDataContent())
                {
                    content.Add(new ByteArrayContent(imageByte), "file", "file.jpg");
                    content.Add(new StringContent(strToPost), "attributes");
                    try
                    {
                        var response = client.PostAsync(serverUriBuilder.Uri.AbsoluteUri, content).Result;
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            string responseString = response.Content.ReadAsStringAsync().Result;
                            JObject result = JObject.Parse(responseString);
                            return result;
                        }
                        else
                        {
                            string responseString = response.Content.ReadAsStringAsync().Result;
                            throw new AddCustomerException(responseString, (int)response.StatusCode);
                        }
                    }
                    catch (Exception e)
                    {
                        throw new AddCustomerException(e.Message);
                    }
                }
            }
        }

        /// <summary>
        /// Перегрузка предыдущего метода для отправки серверу только изображения
        /// </summary>
        /// <param name="serverAddress">Адрес сервера</param>
        /// <param name="token">Токен авторизации</param>
        /// <param name="filename">Имя файла</param>
        /// <returns>Возвращает ответ сервера</returns>

        public JObject Identify(string serverAddress, string token, string filename)
        {
            UriBuilder serverUriBuilder = new UriBuilder(serverAddress);
            serverUriBuilder.Path = "/identify";

            byte[] imageByte = File.ReadAllBytes(filename);
            File.Delete(filename);
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                using (var content = new MultipartFormDataContent())
                {
                    content.Add(new ByteArrayContent(imageByte), "file", "file.jpg");
                    try
                    {
                        var response = client.PostAsync(serverUriBuilder.Uri.AbsoluteUri, content).Result;
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            string responseString = response.Content.ReadAsStringAsync().Result;
                            JObject result = JObject.Parse(responseString);
                            return result;
                        }
                        else
                        {
                            string responseString = response.Content.ReadAsStringAsync().Result;
                            throw new IdentifyException(responseString, (int)response.StatusCode);
                        }
                    }
                    catch (Exception e)
                    {
                        throw new IdentifyException(e.Message);
                    }
                }
            }
            
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

        /// <summary>
        /// Метод получения токена авторизации от сервера
        /// </summary>
        /// <param name="login">ЛОгин</param>
        /// <param name="password">Пароль</param>
        /// <param name="serverAddress">Адрес сервера</param>
        /// <returns></returns>
        public static string Auth(string login, string password, string serverAddress)
        {
            UriBuilder serverUriBuilder = new UriBuilder(serverAddress);
            serverUriBuilder.Path= "/auth";

            JObject body = new JObject
            {
                { "login", login },
                { "password", password }
            };

            var content = new StringContent(body.ToString(Formatting.None), Encoding.UTF8, "application/json");

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage responseMessage = client.PostAsync(serverUriBuilder.Uri, content).Result;
                    if (responseMessage.StatusCode == HttpStatusCode.OK)
                    {
                        JObject result = JObject.Parse(responseMessage.Content.ReadAsStringAsync().Result);
                        return result.Value<string>("token");
                    }
                    else
                    {
                        throw new AuthException(responseMessage.Content.ReadAsStringAsync().Result, (int)responseMessage.StatusCode);
                    }
                }
                catch (Exception e)
                {
                    throw new AuthException($"{e.Message}, {e.StackTrace}", -1);
                }
            }
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
