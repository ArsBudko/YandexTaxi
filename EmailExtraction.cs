//using Aspose.Pdf.Facades;
using HtmlAgilityPack;
using iTextSharp.text;
using iTextSharp.text.pdf;
using MailKit.Net.Imap;
using MailKit.Search;
using OpenHtmlToPdf;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace YandexTaxiParser
{
    public class EmailExtraction
    {
        public string Host;
        public string Login;
        public string Pass;

        public string Path;

        public DateTime StartDay;
        public DateTime EndDay;
        public int i;
        public bool Enable;

        private EmailCounter emailCounter;

        public EmailExtraction(string host, string login, string pass, string path, DateTime start, DateTime end)
        {
            Host = host;
            Login = login;
            Pass = pass;
            Path = path;
            StartDay = start;
            EndDay = end;
            emailCounter = new EmailCounter();
        }

        public async void Execute(IProgress<EmailCounter> progress, Control butten1, Control log)
        {
            progress.Report(emailCounter);

            await Task.Factory.StartNew(() => { GetMails(progress, log); });
            
            butten1.Enabled = true;

            Form1.gui.WriteLog("Завершено.", Color.Black);
        }

        public void GetMails(IProgress<EmailCounter> progress, Control log)
        {
            try
            {
                using (var client = new ImapClient())
                {
                    //проверяем на валидность строку логина
                    Regex regex = new Regex(@"^\S+\@\w+\.(ru|com)$");
                    MatchCollection matches = regex.Matches(Login);

                    if (matches.Count != 1)
                    {
                        Form1.gui.WriteLog("Введен неверный формат данных в строке login ", Color.Red);
                        //_logger.Warn($"Адрес OPC не соответствует ожидаемому формату для {element.Parent.Name}.{element.Name}|{attributeName}");
                        return;
                    }
                    client.Connect(Host, 993, true);
                    client.Authenticate(Login, Pass);
                    client.Inbox.Open(MailKit.FolderAccess.ReadOnly);

                    Form1.gui.WriteLog("Подключение установлено");

                    Form1.gui.WriteLog("Выполняется поиск....");

                    //выгружаем письма по времени
                    SearchQuery query = SearchQuery.FromContains("no-reply@taxi.yandex.ru"); //.And(MailKit.Search.SearchQuery.SubjectContains("Яндекс Go – отчёт о поездке"));
                    var uids = client.Inbox.Search
                        (SearchQuery.DeliveredAfter(StartDay).And
                        (SearchQuery.DeliveredBefore(EndDay.AddDays(1)))
                        //(SearchQuery.SubjectContains("Яндекс Go – отчет о поездке"))
                        //(SearchQuery.FromContains("@taxi.yandex.ru")).And
                        );
                    var cnt = uids.Count();


                    //var massages = client.Inbox.GetMessage.Fetch(uids, MessageSummaryItems.Envelope | MessageSummaryItems.BodyStructure);
                    
                    emailCounter.TotalEmails = cnt;
                    progress.Report(emailCounter);

                    string serverPath = $"{Path}\\Pages\\";

                    var dirPages = new DirectoryInfo(serverPath);


                    if (!dirPages.Exists)
                        dirPages.Create();
                    else
                    {
                        if (dirPages.GetFiles().Any())
                        {
                            dirPages.Delete(true);
                            dirPages.Create();
                        }
                    }

                    string pdfTrackPath = System.IO.Path.Combine(@serverPath, "{0}.Маршрут");
                    string pdfCheckPath = System.IO.Path.Combine(@serverPath, "{0}.Чек на {1}₽");
                    string pdfResultFile = Path + "\\" + "Отчет по поездкам за {0} - {1} на {2}₽" + " id=" + DateTime.Now.Ticks.GetHashCode().ToString("x").ToUpper();

                    if (uids == null || cnt == 0)
                    {
                        Form1.gui.WriteLog("Не найдено ни одного Email в заданном промежутке времени.", Color.Blue);
                        return;
                    }
                    Form1.gui.WriteLog($"Обнаружено {cnt} email в заданном интервале");
                    Form1.gui.WriteLog("Выполняется обработка email от Yandex такси.....");

                    var n = 0;
                    var i = 1;
                    var cntReports = 0;
                    var fullDataTimeTrack = new Dictionary<DateTime, int>();

                    double totalCostTrack = 0;
                    foreach (var uid in uids)
                    {
                        emailCounter.CntEmails++;
                        progress.Report(emailCounter);

                        var message = client.Inbox.GetMessage(uid);

                        if (!message.Subject.StartsWith("Яндекс Go – отчёт о поездке"))
                            continue;
                        try
                        {

                            var messageHtml = client.Inbox.GetMessage(uid).HtmlBody;
                            var pdf_1 = Pdf.From(messageHtml).Content();

                            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                            doc.LoadHtml(messageHtml);
                            //дата поездки
                            var trackDate = doc.DocumentNode.SelectNodes("//tr[@class='report__row']")
                                              .Where(x => x.SelectSingleNode("td").InnerText == "Дата")
                                              .Select(p => p.SelectSingleNode("td[@class='report__value']"))
                                              .FirstOrDefault().InnerText.TrimEnd(new char[] { '.' });

                            if (string.IsNullOrEmpty(trackDate))
                            {
                                Form1.gui.WriteLog($"Не удалось определить дату поездки для отчета с порядковым номером - {i}", Color.Red);
                                continue;
                            }


                            var pathFileTreck = new StringBuilder().AppendFormat($"{pdfTrackPath} {trackDate}.pdf", i).ToString();


                            var deliveryTimeCar = doc.DocumentNode.SelectNodes("//td[@class='route__point-details']//p[@class='hint']")
                                              .FirstOrDefault().InnerText
                                              ;
                           
                            if(string.IsNullOrEmpty(deliveryTimeCar))
                            {
                                Form1.gui.WriteLog($"Не удалось определить время начала поездки для отчета с порядковым номером - {i}", Color.Red);
                                continue;
                            }

                            var time = DateTime.ParseExact($"{trackDate} {deliveryTimeCar}", "d MMMM yyyy 'г' HH:mm",
                                       CultureInfo.GetCultureInfo("ru-RU"));

                            if (fullDataTimeTrack.TryGetValue(time, out int num))
                            {
                                Form1.gui.WriteLog($"Обнаружен дубликат отчета о поезде на дату {trackDate} с порядковыми номерами - {num} и {i}. " +
                                    Environment.NewLine +
                                    $"В финальный отчет попадет поездака с порядковыми номером - {num}.", Color.Orange);
                                continue;                            
                            }
                         
                            //ссылка на чек поездки
                            var hrefList = doc.DocumentNode.SelectNodes("//a[@class='report-link']")
                                              .Where(x => x.InnerText.Contains("Чек поездки"))
                                              .Select(p => p.GetAttributeValue("href", "not found"))
                                              .ToList().FirstOrDefault();

                            WebRequest req = HttpWebRequest.Create(hrefList);
                            req.Method = "GET";

                            string checkHtml;
                            using (StreamReader reader = new StreamReader(req.GetResponse().GetResponseStream()))
                            {
                                checkHtml = reader.ReadToEnd();
                            }

                            //стоимость поездки поездки

                            HtmlAgilityPack.HtmlDocument docCheck = new HtmlAgilityPack.HtmlDocument();
                            docCheck.LoadHtml(checkHtml);

                            var costTrack = docCheck.DocumentNode.SelectNodes("//tr[@class='totals-row']")
                                                .Where(x => x.SelectSingleNode("td").InnerText == "Итого")
                                                .Select(p => double.Parse(p.LastChild.InnerText.TrimEnd(new char[] { '₽' })
                                                .Trim()))
                                                .Sum();


                            var pdf_2 = Pdf.From(checkHtml).Content();

                            var pathFileCheck = new StringBuilder().AppendFormat($"{pdfCheckPath} {trackDate}.pdf", i, costTrack).ToString();

                            totalCostTrack += costTrack;

                            File.WriteAllBytes(pathFileTreck, pdf_1.ToArray());
                            File.WriteAllBytes(pathFileCheck, pdf_2.ToArray());

                            emailCounter.TaxiEmails++;

                            cntReports++;

                            fullDataTimeTrack.Add(time, i);
                        }
                        catch (Exception ex)
                        {
                            Form1.gui.WriteLog($"Ошибка инициализации отчета по поездке с порядковым номером - {i}", Color.Red);
                        }
                        finally
                        {
                            i++;
                            progress.Report(emailCounter);
                            //reports.Report(cntReports.ToString());
                        }

                    }

                    if (emailCounter.TaxiEmails == 0)
                    {
                        Form1.gui.WriteLog($"Не обнаружено корректных email отчетов от Yandex такси на заданном интервале запроса.", Color.Blue);
                        return;
                    }

                    var fullDataTimeTrackSort = fullDataTimeTrack.OrderBy(t => t.Key).ToArray();

                    Form1.gui.WriteLog($"Всего обнаружено {cntReports} корректных email отчетов от Yandex такси.", Color.Black);

                    var pathFileResult = new StringBuilder().AppendFormat($"{pdfResultFile}.pdf", StartDay.ToString("dd/MM"), EndDay.ToString("dd/MM"), totalCostTrack).ToString();

                    Form1.gui.WriteLog($"Выполняется процесс сборки отчетов в единый pdf.", Color.Black);
                    var FI = dirPages.GetFiles("*.pdf").ToArray();
                    var FIdir = FI.GroupBy(x => int.Parse(x.Name.Split(new char[] { '.' }).First())).ToDictionary(x =>x.Key, x=>x.Select(y => y.FullName).ToArray());
                    
                    Form1.gui.WriteLog($"Сортировка поездок по времени.", Color.Black);
                    var filesResult = new List<string>();
                    foreach (var f in fullDataTimeTrackSort)
                    {
                        if (FIdir.TryGetValue(f.Value, out var r))
                            filesResult.AddRange(r);
                    }

                    Union(filesResult.ToArray(), pathFileResult);

                    Form1.gui.WriteLog($"Имя файла {System.IO.Path.GetFileName(pathFileResult)}.", Color.Green);
                }
            }
            catch (Exception ex)
            {
                Form1.gui.WriteLog("Ошибка", Color.Red);
                Form1.gui.WriteLog($"{ex.Message}", Color.Red);
            }
        }

        private void Union(string[] pdfPaths, string outputPdfPath)
        {
            //string[] lstFiles = new string[3];
            //lstFiles[0] = @"C:/pdf/1.pdf";
            //lstFiles[1] = @"C:/pdf/2.pdf";
            //lstFiles[2] = @"C:/pdf/3.pdf";

            PdfReader reader = null;
            Document sourceDocument = null;
            PdfCopy pdfCopyProvider = null;
            PdfImportedPage importedPage;
            //string outputPdfPath = @"C:/pdf/new.pdf";


            sourceDocument = new Document();
            pdfCopyProvider = new PdfCopy(sourceDocument, new System.IO.FileStream(outputPdfPath, System.IO.FileMode.Create));

            //Open the output file
            sourceDocument.Open();

            try
            {
                //Loop through the files list
                for (int f = 0; f < pdfPaths.Length; f++)
                {
                    int pages = get_pageCcount(pdfPaths[f]);

                    reader = new PdfReader(pdfPaths[f]);
                    //Add pages of current file
                    for (int i = 1; i <= pages; i++)
                    {
                        importedPage = pdfCopyProvider.GetImportedPage(reader, i);
                        pdfCopyProvider.AddPage(importedPage);
                    }

                    reader.Close();
                }
                //At the end save the output file
                sourceDocument.Close();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private int get_pageCcount(string file)
        {
            using (StreamReader sr = new StreamReader(File.OpenRead(file)))
            {
                Regex regex = new Regex(@"/Type\s*/Page[^s]");
                MatchCollection matches = regex.Matches(sr.ReadToEnd());

                return matches.Count;
            }
        }
    }
}
