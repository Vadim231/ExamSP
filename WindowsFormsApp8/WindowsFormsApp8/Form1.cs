using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp8
{
    public partial class Form1 : Form
    {
        //Semaphore sema = new Semaphore(20,20);
        bool isStopped = false;


        //для потоков
        public static int thrCount = 0;
        public static ManualResetEvent mre;


        //общий размер всех файлов
        public static double totalSize = 0;

        //папка в которой искать
        DirectoryInfo directory = null;

        //словарь со словами
        FileInfo badFile = null;
        public static string[] badWords = null;
        public List<String> slova = new List<string>();

        //папка для отчета
        static public DirectoryInfo repDirectory = null;

        Thread thSearch = null;


        public Form1()
        {
            InitializeComponent();
            pbFiles.Minimum = 0;
            pbFiles.Step = 100;
        }

        public void btnStartClick(object sender, EventArgs e)
        {
            //Отчет
            thSearch = new Thread(SearchRoutine);

            btnStop.Enabled = true;
            btnStart.Enabled = true;

            thSearch.IsBackground = true;
            thSearch.Start();
        }

        public void SearchRoutine()
        {
            mre = new ManualResetEvent(false);
            //Словарик
            if (badFile == null)
            {
                MessageBox.Show("Сначала выберите словарик", "Внимание!");
                return;
            }
            using (StreamReader sr = new StreamReader(badFile.FullName))
            {
                while (!sr.EndOfStream)
                    slova.Add(sr.ReadLine());
            }
            foreach (var word in slova)
            {
                badWords = word.Split(' ');
            }


            //создаем DirectoryInfo и указываем введенный в textBox путь.
            if (directory == null)
            {
                MessageBox.Show("Сначала выберите папку!", "Внимание!");
            }
            else
            {
                try
                {
                    GetDirectorySize(directory);

                    //MessageBox.Show("Общий размер файлов: " + totalSize.ToString() + " байт");
                    
                    

                    //pbFiles.Maximum = Convert.ToInt32(totalSize);
                    pbFiles.Invoke(new Action<int>((x) => { pbFiles.Maximum = x; pbFiles.Update(); }), Convert.ToInt32(totalSize));

                    //pbFiles.Invoke(new Action<int>((x) => { pbFiles.Maximum = x; pbFiles.Update(); }),Convert.ToInt32(999000000));


                    WalkDirectoryTree(directory, pbFiles);

                    //конец работы

                    btnStop.Invoke(new Action<bool>((x) => { btnStop.Enabled = x;}), false);

                    btnStart.Invoke(new Action<bool>((x) => { btnStart.Enabled = x; }), false);
                    
                    DialogResult res=MessageBox.Show("Все файлы просмотренны, открыть отчет?","Работа завершена",MessageBoxButtons.OKCancel);
                    if (res == DialogResult.OK)
                    {
                        Process.Start(repDirectory + @"\report.txt");
                    }
                    else
                    {
                        Close();
                    }

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "SearchRoutine");
                }
            }
        }


        //Проходится по всем файлам и подпапкам в указанной папке
        static public void WalkDirectoryTree(DirectoryInfo root,ProgressBar progress)
        {
            System.IO.FileInfo[] files = null;
            System.IO.DirectoryInfo[] subDirs = null;

            try
            {
                files = root.GetFiles("*.txt");
            }
            catch (Exception){ }
            if (files != null)
            {
                foreach (FileInfo file in files)
                {
                    //полный путь файла
                    string fileFullName = file.FullName;
                    //размер файла
                    double fileSize = Convert.ToDouble(file.Length);
                    try
                    {
                        progress.Invoke(new Action<int>((x) => { progress.Value += x; progress.Update(); }), Convert.ToInt32(fileSize));
                    }
                    catch (Exception) { }
                    try
                    {
                        //просматриваем каждый файл на наличие ПЛОХОГО СЛОВА

                        string text2 = "";
                        string copyText="";
                        string text = File.ReadAllText(fileFullName);
                        text2 = text.ToLower();

                        foreach (var word in badWords)
                        {
                            if (text2.Contains(word))
                            {
                                //записываю в отчет инфу
                                File.AppendAllText(repDirectory + @"\report.txt", "Путь файла: " + fileFullName+" Размер файла: "+file.Length + " Слово: " + word+"\r\n");
                                //заменяем это слово на *****
                                text2 = text2.Replace(word, "****");
                                copyText = text2;

                                //сохраняем файл с измененным текстом
                                File.WriteAllText(repDirectory.FullName + @"\" + file.Name, copyText);
                            }
                            
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "WalkDirectoryTree");
                    }
                }
                //подпапка
                subDirs = root.GetDirectories();
                foreach (DirectoryInfo dirInfo in subDirs)
                {
                    WalkDirectoryTree(dirInfo,progress);
                }
            }
        }

        static public void GetDirectorySize(DirectoryInfo root)
        {
            System.IO.FileInfo[] files = null;
            System.IO.DirectoryInfo[] subDirs = null;

            try
            {
                files = root.GetFiles("*.txt");
            }
            catch (Exception) { }
            if (files != null)
            {
                foreach (FileInfo file in files)
                {
                    //полный путь файла
                    string fileFullName = file.FullName;
                    //размер файла
                    double fileSize = Convert.ToDouble(file.Length);

                    totalSize += fileSize;
                    //progress.Invoke(new Action<int>((x) => { progress.Value = x; progress.Update(); }), Convert.ToInt32(fileSize));
                }
                //подпапка
                subDirs = root.GetDirectories();
                foreach (DirectoryInfo dirInfo in subDirs)
                {
                    GetDirectorySize(dirInfo);
                }
            }
        }



        //кнопка Close
        private void btnCloseClick(object sender, EventArgs e)
        {
            Close();
        }

        private void btnBrowseClick(object sender, EventArgs e)
        {
            //выбрать папку, в которой будет поиск
            FolderBrowserDialog folder = new FolderBrowserDialog();
            //folder.RootFolder =  Environment.SpecialFolder.MyComputer;
            try { folder.SelectedPath = @"D:\Домашки\test"; }
            catch (Exception)
            {
                folder.SelectedPath = "C:\\";
            }
            if (folder.ShowDialog() == DialogResult.OK)
            {
                txtDir.Text=folder.SelectedPath;
                directory = new DirectoryInfo(folder.SelectedPath);
            }
        }

        private void btnBrowse2Click(object sender, EventArgs e)
        {
            //выбор файла с плохими словами
            OpenFileDialog file=new OpenFileDialog();
            if (file.ShowDialog() == DialogResult.OK)
            {
                txtBad.Text = file.FileName;
                badFile = new FileInfo(file.FileName);
            }
        }

        private void btnBrowse3Click(object sender, EventArgs e)
        {
            //выбрать папку, в которой будет находиться отчет
            FolderBrowserDialog folder = new FolderBrowserDialog();
            try { folder.SelectedPath = @"D:\Домашки"; }
            catch (Exception)
            {
                folder.SelectedPath = "C:\\";
            }
            if (folder.ShowDialog() == DialogResult.OK)
            {
                txtReport.Text = folder.SelectedPath;
                repDirectory = new DirectoryInfo(folder.SelectedPath);
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            thrCount++;
            if (thrCount % 2 != 0)
            {
                MessageBox.Show(thSearch.ThreadState.ToString());
                try
                {
                    thSearch.Suspend();
                }
                catch (Exception) { }

                btnStop.Text = "Resume";
            }
            else
            {
                try
                {
                    thSearch.Resume();
                }
                catch (Exception) { }
                

                btnStop.Text = "Stop";
            }
        }
    }
}
