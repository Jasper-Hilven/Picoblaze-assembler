using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Windows.Forms;

namespace PicoBlazeCompiler
{
    public partial class TestForm : Form
    {
        public TestForm()
        {
            InitializeComponent();
        }

        public TextBox getTextbox1()
        {
            return textBox1;
        }
        public TextBox getTextbox2()
        {
            return textBox2;
        }
        public TextBox getTextbox3()
        {
            return textBox3;
        }

        private void TestForm_Load(object sender, EventArgs e)
        {

        }

        public string getCleanTemplate()
        {
            return System.IO.File.ReadAllText("C:\\Users\\JASPER\\Documents\\programmeren\\ROM_form.vhd");




        }

        private void button1_Click(object sender, EventArgs e)
        {
            string code = getTextbox1().Text;
            var compiler = new Compiler(code, getCleanTemplate());
            compiler.myForm = this;
            try
            {
                string outstring;
                getTextbox2().Text = compiler.Compile(out outstring);
                getTextbox3().Text = outstring;

            }
            catch(CompilerException exception)
            {
                textBox2.Text = exception.line + ":  "+exception.message;
            }
          
        }

        private void button2_Click(object sender, EventArgs e)
        {
            textBox1.Text = "";
        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                MailMessage mail = new MailMessage();
                SmtpClient SmtpServer = new SmtpClient("smtp.gmail.com");

                mail.From = new MailAddress("jhsender@gmail.com");
                mail.To.Add(textBox4.Text);
                mail.Subject = "Pico Code!";
                mail.Body = textBox3.Text;

                SmtpServer.Port = 587;
                SmtpServer.Credentials = new System.Net.NetworkCredential("mailreplyno@gmail.com", "mailreplynowachtwoord");
                SmtpServer.EnableSsl = true;

                SmtpServer.Send(mail);
                MessageBox.Show("mail Send");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            StreamReader sr = new StreamReader(textBox5.Text);
            textBox1.Text = sr.ReadToEnd();
            sr.Close();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            button4_Click(null,null);
            button1_Click(null, null);
            button3_Click(null, null);
        }
        public TextBox getTextbox6()
        {
            return textBox6;
        }
    }
}
