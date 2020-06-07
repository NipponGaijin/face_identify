using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.UI;
using Emgu.CV.Cvb;
using Emgu.Util;
using System.Net;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace faceRecognition
{

    public partial class FormIdentify : Form
    {
        
        List<string> filenames;
        // Объявление форм
        FormCalibrateAndSetServer formSettings = new FormCalibrateAndSetServer();
        FormAddToDB formAddToDB = new FormAddToDB();
        FormFullTextSearch formFullTextSearch = new FormFullTextSearch();

        Capture capWebcam = null;
        getWebCam getWebCam = new getWebCam();
        HaarCascade cascade = new HaarCascade("haarcascade_frontalface_default.xml"); //Объявление каскада Хаара
        bool readyToIdentify = false;
        bool readyToNewFace = false;
        bool photoSaved = false;
        bool addingImageStarted = false;
        Task updateRecordTask;
        Task deleteRecordTask;
        Task sendPostRequestTask;
        static bool requestIsStarted;
        static int idInDb = 0;
        static bool stringResponsed = false;
        static bool buttonEnable = true;
        static string responsedString;
        public int maxFace;
        public int minFace;
        public string address = "";
        int currentIndex = 0;

        public FormIdentify()
        {
            InitializeComponent();
        }

        //Обработка действия загрузки формы
        private void FormIdentify_Load(object sender, EventArgs e)
        {
            
            if (!File.Exists("settings.json")) //если файла с настройками нет, запускается форма настроек
            {
                formSettings.identifyForm = this;
                formSettings.ShowDialog();
            }
            else
            {
                settingsLoad();
            }

            try
            {
                capWebcam = getWebCam.setCam();               // инициализируем объект записи с дефолтной вебки
            }
            catch (NullReferenceException exept)       // ошибка, если запись неудалась
            {
                return;
            }
            //Инициализация таблицы
            dataTableIndent.ColumnCount = 2;
            dataTableIndent.Columns[0].Name = "Название поля";
            dataTableIndent.Columns[1].Name = "Содержание поля";
            dataTableIndent.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataTableIndent.AllowUserToAddRows = false;
            toolButtonPanel.Visible = false;
            //Инициализация веб-камеры
            if (capWebcam != null)
            {
                Application.Idle += processFrameAndUpdGui;
            }
            //else //если камера не инициализирована, ставится заглушка
            //{
            //    VideoBox.Image = new Image<Bgr, Byte>("Content//cup.png");
            //}
        }
        private void FormIdentify_FormClosed(object sender, FormClosedEventArgs e)
        {
            //Действие по закрытию формы
            settingsSave();
            if (capWebcam != null)
            {
                getWebCam.disposeCam();
            }
            if (File.Exists("savedIdentifyFrame.jpg"))
            {
                File.Delete("savedIdentifyFrame.jpg");
            }
            deleteAllDownloadedFiles();
        }

        private void processFrameAndUpdGui(object sender, EventArgs e)
        {
            //Функция обрабатывающая кадры и обновляющая GUI

            //Объявление переменных с изображениями
            Image<Bgr, Byte> image = capWebcam.QueryFrame(); //.Resize(imageBox1.Width, imageBox1.Height, INTER.CV_INTER_LANCZOS4)
            Image<Gray, Byte> grayImage = image.Convert<Gray, Byte>();
            //Ищем признаки лица
            MCvAvgComp[][] Faces = grayImage.DetectHaarCascade(cascade, 1.2, 1, HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, new Size(maxFace, maxFace));
            //Устанавливаем toolButtonPanel активной, если идентификация прошла
            if (updateRecordTask != null && !updateRecordTask.IsCompleted)
            {
                toolButtonPanel.Enabled = false;
            }
            else
            {
                toolButtonPanel.Enabled = true;
            }
            //Если запрос стартовал, выключаем кнопки
            buttonIdentify.Enabled = !requestIsStarted;
            btnAddNewImage.Enabled = !addingImageStarted;

            //Если лиц не найдено
            if (Faces[0].Length == 0)
            {
                lblInformation.Text = "Дождитесь, пока лицо будет обведено рамкой";
                readyToIdentify = false;
            }
            else
            {
                readyToIdentify = true;
            }

            //Если лиц больше одного
            if (Faces[0].Length > 1)
            {
                lblInformation.Text = "В кадре больше одного лица";
                readyToIdentify = false;
            }
            //Отрисовываем границы лица
            foreach (MCvAvgComp face in Faces[0])
            {
                
                if (Faces[0].Length > 0 && Faces[0].Length < 2)
                {
                    lblInformation.Text = "";
                }
                image.Draw(face.rect, new Bgr(Color.Green), 6);
            }
            //Выводим обработаное приложение
            VideoBox.Image = image;
            //Если идентификация прошла, обновляем таблицу с данными
            if (stringResponsed)
            {
                lblSearching.Text = "";
                updateTable();
                stringResponsed = false;
            }
        }

        private void updateTable()
        {
            //Функция обновленя таблицы

            if (responsedString != "" && responsedString != null)
            {
                currentIndex = 0;
                lblFoundStatus.Text  = "Пользователь найден";
                //Десериализуем JSON строку
                var jsonDict = new JavaScriptSerializer().Deserialize<Dictionary<string, dynamic>>(responsedString);
                idInDb = jsonDict["id"];
                //Формирование списка с именами файлов
                filenames = new List<string>(jsonDict["filenames"].Keys);
                //Устанавливаем изображение в поле IdentifyedUser
                Image<Rgb, Byte> downlodadedImage = new Image<Rgb, Byte>(@"images\" + filenames[currentIndex]);
                IdentifyedUser.Image = downlodadedImage;
                //Формирование списка названий полей
                List<string> listOfKeys = new List<string>(jsonDict["content"].Keys);
                dataTableIndent.Rows.Clear();
                toolButtonPanel.Visible = false;
                //Устанавливаем значания в таблицу
                for (int i = 0; i < jsonDict["content"].Count; i++)
                {
                     dataTableIndent.Rows.Add();
                     dataTableIndent["Название поля", i].Value = (String)listOfKeys[i];
                     dataTableIndent["Содержание поля", i].Value = (String)jsonDict["content"][listOfKeys[i]];
                }
            }
            else if (responsedString == "")
            {
                dataTableIndent.Rows.Clear();
                toolButtonPanel.Visible = false;
                lblFoundStatus.Text = "Пользователь не найден, добавьте его";
            }
            responsedString = null;
            

        }

        async void sendPostRequest()
        {
            //Функция отправки POST запроса (выполняется асинхронно)
            requestIsStarted = true;
            try
            {
                WebTools postClass = new WebTools(); //Экземпляр класса WebTool

                string responseFromServer = postClass.postImageToServer(address, "savedIdentifyFrame.jpg", "identify_and_search_user");
                
                string[] resultOfRequest = System.Text.RegularExpressions.Regex.Split(responseFromServer, @"<content>");
                if (resultOfRequest[0] == "Succsess POST")
                {
                    //Скачивание файлов с сервера (имена файлов берутся из JSON строки)
                    var jsonDict = new JavaScriptSerializer().Deserialize<Dictionary<string, dynamic>>(resultOfRequest[1]);
                    List<string> listOfKeys = new List<string>(jsonDict["filenames"].Keys);
                    WebTools downloadTool = new WebTools();
                    for (int i = 0; i < listOfKeys.Count; i++)
                    {
                        downloadTool.downloadFileFromServer(listOfKeys[i], address);
                    }
                        
                    requestIsStarted = false;
                    //buttonEnable = true;
                    stringResponsed = true;
                    responsedString = resultOfRequest[1];
                }
                else if (resultOfRequest[0] == "NOTFOUND")
                {
                    responsedString = "";
                    requestIsStarted = false;
                    //buttonEnable = true;
                    stringResponsed = true;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                buttonEnable = true;
                requestIsStarted = false;
            }

        }

        private void settingsSave()
        {
            //Функция сохранения настроек в JSON файл

            Dictionary<string, dynamic> dict = new Dictionary<string, dynamic>();
            dict.Add("address", address);
            dict.Add("maxFace", maxFace);
            dict.Add("minFace", minFace);
            string jsonString = new JavaScriptSerializer().Serialize(dict);
            File.WriteAllText("settings.json", jsonString);
        }
        private void settingsLoad()
        {
            //Функция загрузки настроек

            var jsonDict = new JavaScriptSerializer().Deserialize<Dictionary<string, dynamic>>(File.ReadAllText("settings.json"));
            address = jsonDict["address"];
            maxFace = jsonDict["maxFace"];
            minFace = jsonDict["minFace"];

        }
        private string formingDataString()
        {
            //Формирование JSON строки для отправки серверу

            bool isNotOk = false;
            //Формирование словаря
            Dictionary<string, dynamic> dictOfTableInfo = new Dictionary<string, dynamic>();
            dictOfTableInfo.Add("id", idInDb);
            dictOfTableInfo.Add("content", new Dictionary<string, dynamic>());

            //Наполнение словаря данными из таблицы
            for (int i = 0; i < dataTableIndent.RowCount; i++)
            {
                if (!String.IsNullOrEmpty((String)dataTableIndent["Содержание поля", i].Value) && !String.IsNullOrEmpty((String)dataTableIndent["Название поля", i].Value))
                {
                    dictOfTableInfo["content"].Add((String)dataTableIndent["Название поля", i].Value, (String)dataTableIndent["Содержание поля", i].Value);
                }
                else
                {
                    MessageBox.Show("В строке " + (i + 1).ToString() + " пустое значение!!!");
                    isNotOk = true;
                    break;
                }

            }
            if (!isNotOk) //если ошибок нет
            {
                string jsonString = new JavaScriptSerializer().Serialize(dictOfTableInfo);
                return jsonString;
            }
            else
            {
                return "NOT OK";
            }
        }

        
        private void addUser_Click(object sender, EventArgs e)
        {
            //Запуск формы formAddToDB

            formAddToDB.address = address;
            formAddToDB.maxFace = maxFace;
            formAddToDB.minFace = minFace;
            formAddToDB.ShowDialog();
            lblFoundStatus.Text = "";
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            //Запуск формы formFullTextSearch

            formFullTextSearch.address = address;
            formFullTextSearch.ShowDialog();
        }

        private void dataTableIndent_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            //Если идентификация прошла и данные в таблице сменились
            toolButtonPanel.Visible = true;
        }

        private void buttonDeleteRecord_Click(object sender, EventArgs e)
        {
            //Обработка нажатия кнопки buttonDeleteRecord

            deleteRecordTask = Task.Run(() => deleteRecord(idInDb)); //Запуск Task
            dataTableIndent.Rows.Clear();
            IdentifyedUser.Image = null;
            toolButtonPanel.Visible = false;
        }        
        async void deleteRecord(int id)
        {
            //Функция удаления записи по id (асинхронно)

            WebTools deletingRecordOnServer = new WebTools(); //Экземпляр класса WebTools
            try
            {
                string response = deletingRecordOnServer.deleteRecordRequest(address, id);
                if (response != "<OK>")
                {
                    MessageBox.Show("Не удалено");
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

        async void updateRecord(int id, string updatedString)
        {
            //Функция обновления записи

            WebTools updatingRecordOnServer = new WebTools();
            try
            {
                string response = updatingRecordOnServer.updateRecordRequest(address, id, updatedString);
                if (response != "<OK>")
                {
                    MessageBox.Show("Не обновлено");
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

        private void buttonUpdateRecord_Click(object sender, EventArgs e)
        {
            //обработчик нажатий кнопки buttonUpdateRecord

            string updatedString = formingDataString();
            if (updatedString != "NOT OK")
            {
                updateRecordTask = Task.Run(() => updateRecord(idInDb, updatedString));
            }

        }

        private void buttonIdentify_Click(object sender, EventArgs e)
        {
            //обработчик нажатий кнопки buttonIdentify

            deleteAllDownloadedFiles();
            if (readyToIdentify)
            {
                try
                {
                    Image<Bgr, byte> imageToPost = capWebcam.QueryFrame();
                    imageToPost.Save("savedIdentifyFrame.jpg");
                    lblFoundStatus.Text = "";
                    lblSearching.Text = "Идет поиск, ожидайте...";
                    sendPostRequestTask = Task.Run(() => sendPostRequest());
                }
                catch (Exception exc) { MessageBox.Show(exc.ToString()); }
            }
        }

        private void clearTable_Click(object sender, EventArgs e)
        {
            //обработчик нажатий кнопки clearTable_Click

            dataTableIndent.Rows.Clear();
            toolButtonPanel.Visible = false;
        }


        private void settings_Click(object sender, EventArgs e)
        {
            //обработчик нажатий кнопки settings

            formSettings.identifyForm = this;
            formSettings.faceMax = maxFace;
            formSettings.faceMin = minFace;
            formSettings.address = address;
            formSettings.ShowDialog();
        }

        private void buttonNext_Click(object sender, EventArgs e)
        {
            if (currentIndex < filenames.Count - 1)
            {
                currentIndex++;
                Image<Bgr, Byte> viewedImage = new Image<Bgr, byte>("images\\" + filenames[currentIndex]);
                IdentifyedUser.Image = viewedImage;
            }
            else
            {
                currentIndex = 0;
                Image<Bgr, Byte> viewedImage = new Image<Bgr, byte>("images\\" + filenames[currentIndex]);
                IdentifyedUser.Image = viewedImage;
            }
        }

        private void buttonPrev_Click(object sender, EventArgs e)
        {
            if (currentIndex == 0)
            {
                currentIndex = filenames.Count - 1;
                Image<Bgr, Byte> viewedImage = new Image<Bgr, byte>("images\\" + filenames[currentIndex]);
                IdentifyedUser.Image = viewedImage;
            }
            else
            {
                currentIndex--;
                Image<Bgr, Byte> viewedImage = new Image<Bgr, byte>("images\\" + filenames[currentIndex]);
                IdentifyedUser.Image = viewedImage;
            }
        }
        private void deleteAllDownloadedFiles()
        {
            //Функция очищащая директорию images

            string[] filesToDelete = Directory.GetFiles("images");
            foreach (var v in filesToDelete)
            {
                File.Delete(v);
            }
        }

        private void btnAddNewImage_Click(object sender, EventArgs e)
        {
            if (idInDb != 0 && readyToIdentify)
            {
                Image<Bgr, Byte> imageToAdd = capWebcam.QueryFrame();
                imageToAdd.Save("imageToAdd.jpg");
                Task addNewImageTask = Task.Run(() => addNewImage(idInDb));
            }
        }
        async void addNewImage(int id)
        {
            //Функция добавления нового изображения

            addingImageStarted = true;
            try
            {
                WebTools addImage = new WebTools();
                string request = addImage.postImageToServer(address, "imageToAdd.jpg", "add_new_image", id.ToString());
                if (request == "<UPDATED>")
                {
                    addingImageStarted = false;
                    MessageBox.Show("Добавлено".ToString());
                }
                else
                {
                    addingImageStarted = false;
                    MessageBox.Show("Не добавлено".ToString());
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                addingImageStarted = false;
            }
        }
    }
}
