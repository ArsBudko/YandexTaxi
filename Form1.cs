using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace YandexTaxiParser
{
    public partial class Form1 : Form
    {
        public static Form1 gui;
        public Form1()
        {
            InitializeComponent();
            gui = this;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;

            WriteLog("Инициализации параметров.", Color.Black);

            string host = textBox1.Text;
            string login = textBox2.Text;
            string pass = textBox3.Text;
            string path = textBox8.Text;
            DateTime startDay = dateTimePicker1.Value.Date;
            DateTime endDay = dateTimePicker2.Value.Date;
            //Control textLog = new Control();


            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(login) ||
                string.IsNullOrWhiteSpace(pass) ||
                string.IsNullOrWhiteSpace(path) ||
                startDay == DateTime.MinValue ||
                endDay == DateTime.MinValue ||
                endDay < startDay)
            {
                WriteLog("Заполните все входные параметры", Color.Red);
                WriteLog("Задача завершена ошибкой", Color.Red);

                button1.Enabled = true;
                return;
            }

            Progress<EmailCounter> progress = new Progress<EmailCounter>(ReportProcessingProgress);

            //Progress<string> reports = new Progress<string>(text => this.label10.Text = text);

            var emailReader = new EmailExtraction(host, login, pass, path, startDay, endDay);

            emailReader.Execute(progress, button1, textLog);
        }

        private void ReportProcessingProgress(EmailCounter obj)
        {
            this.progressBar1.Value = obj.CntEmails;
            this.progressBar1.Maximum = obj.TotalEmails;
            this.label10.Text = obj.TaxiEmails.ToString();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        public void textBox5_TextChanged(object sender, EventArgs e)
        {
           
        }

        private void label9_Click(object sender, EventArgs e)
        {

        }

        private void textBox8_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {

        }

        private void dateTimePicker2_ValueChanged(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox8.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void label10_Click(object sender, EventArgs e)
        {

        }

        public void log(string text, Color color)
        {
            richTextBox1.SelectionColor = color;
            richTextBox1.SelectedText = text;
            
        }
        public void log(string text)
        {
            richTextBox1.SelectionColor = Color.Black;
            richTextBox1.SelectedText = text;

        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            richTextBox1.ScrollToCaret();

        }

        private void textLog_TextChanged(object sender, EventArgs e)
        {           
            log(textLog.Text + "\r\n", textLog.ForeColor);
            textLog.Clear();
        }

        public void WriteLog(string log, Color color)
        {
            //textLog.Clear();
            textLog.ForeColor = color;
            this.Invoke(new Action(() => { textLog.Text += log; }));

        }
        public void WriteLog(string log)
        {
            //textLog.Clear();

            this.Invoke(new Action(() => { textLog.Text += log; }));

        }

        private void button3_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
        }

        private void progressBar1_Click(object sender, EventArgs e)
        {

        }
    }
}
