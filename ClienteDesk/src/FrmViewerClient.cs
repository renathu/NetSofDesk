using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClienteDesk.src
{
    public partial class FrmViewerClient : Form
    {
        public SslStream targetSslStream { get; set; }

        public TcpClient targetTcpClient { get; set; }

        private Thread thread;
        private bool threadBreak;

        public FrmViewerClient()
        {
            InitializeComponent();
        }

        private void FrmViewerClient_Load(object sender, EventArgs e)
        {
            thread = new Thread(() =>
            {
                while (threadBreak == false)
                {
                    try
                    {
                        var lengthBuffer = new byte[4];
                        int bytesRead = targetSslStream.Read(lengthBuffer, 0, lengthBuffer.Length);
                        if (bytesRead == 0)
                        {
                            Thread.Sleep(TimeSpan.FromSeconds(2).Microseconds);
                            continue;
                        }

                        int imageSize = BitConverter.ToInt32(lengthBuffer, 0);
                        var imageData = new byte[imageSize];
                        int bytesReadImage = 0;
                        while (bytesReadImage < imageSize)
                        {
                            bytesRead = targetSslStream.Read(imageData, bytesReadImage, imageSize - bytesReadImage);
                            bytesReadImage += bytesRead;
                        }

                        using (var ms = new MemoryStream(imageData))
                        {
                            if (pictureBox1.InvokeRequired)
                            {
                                Invoke(new Action(() => pictureBox1.Image = new Bitmap(ms)));
                            }
                            else
                            {
                                pictureBox1.Image = new Bitmap(ms);
                            }
                        }
                    }
                    catch (Exception ex) 
                    {
                        threadBreak = true;
                        break;
                    }
                }
            });
            thread.Start();            
        }

        private void FrmViewerClient_KeyDown(object sender, KeyEventArgs e)
        {

        }

        private void FrmViewerClient_FormClosing(object sender, FormClosingEventArgs e)
        {
            targetTcpClient?.Close();
        }
    }
}
