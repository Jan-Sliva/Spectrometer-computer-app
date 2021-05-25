using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Ports;
using System.Threading;

namespace Spectrometer_computer_app
{
    public partial class Form1 : Form
    {
        int Rows = 240;
        int Columns = 320;
        int BytesPerPixel = 2;

        public Form1()
        {
            InitializeComponent();
        }


        private void Form1_Load(object sender, System.EventArgs e)
        {
            serialPort1 = new SerialPort();
            serialPort1.BaudRate = 500000;
            serialPort1.DataBits = 8;
            serialPort1.StopBits = StopBits.One;
            serialPort1.Handshake = Handshake.None;
            serialPort1.Parity = Parity.None;
            serialPort1.ReadBufferSize = 200000;

            string[] ports = SerialPort.GetPortNames();
            comboBox1.Items.AddRange(ports);
            comboBox1.SelectedIndex = 0;

            var _blankBitmap = new Bitmap(Columns, Rows, PixelFormat.Format16bppRgb565);
            pictureBox1.Image = (Image)_blankBitmap.Clone();
            pictureBox2.Image = (Image)_blankBitmap.Clone();
        }

        private void Form1_Closed(object sender, FormClosedEventArgs e)
        {

            if (serialPort1.IsOpen)
            {
                serialPort1.Write(new byte[] { 2 }, 0, 1);
                StopStreaming();
            }
        }

        private void StartButton_Click(object sender, System.EventArgs e)
        {
            if (!serialPort1.IsOpen)
            {
                serialPort1.DataReceived += DataReceivedHandler;
                serialPort1.PortName = comboBox1.Text;
                try
                {
                    serialPort1.Open();
                }
                catch (IOException) { }
            }

            if (serialPort1.IsOpen)
            {
                serialPort1.DiscardInBuffer();
                serialPort1.Write(new byte[]{ 1 }, 0, 1);
                label2.Text = "Connected to port " + comboBox1.Text;
            }

        }

        private void EndButton_Click(object sender, System.EventArgs e)
        {
            if (serialPort1.IsOpen)
            {
                serialPort1.Write(new byte[] { 2 }, 0, 1);
                StopStreaming();
            }
            label2.Text = "Not connected";
        }

        private void StopStreaming()
        {
            serialPort1.DataReceived -= DataReceivedHandler;
            Thread.Sleep(1000);
            serialPort1.DiscardInBuffer();
            Thread.Sleep(1000);
            serialPort1.Close();
        }

        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e){
            SerialPort port = (SerialPort)sender;

            byte[] Buffer = new byte[Rows * Columns * BytesPerPixel];
            Bitmap Bitmap = new Bitmap(Columns, Rows, PixelFormat.Format16bppRgb565);

            int position = 0;
            int plusPosition = 0;

            bool show = true;

            try
            {
                while (position < Rows * Columns * BytesPerPixel)
                {
                    plusPosition = port.BytesToRead;
                    port.Read(Buffer, position, Math.Min(Rows * Columns * BytesPerPixel - position, plusPosition));
                    position += plusPosition;
                }
            }
            catch (InvalidOperationException)
            {
                show = false;
            }
            catch (TimeoutException)
            {
                show = false;
            }

            if (show)
            {
                for (int row = 0; row < Rows; row++)
                {

                    for (int column = 0; column < Columns; column++)
                    {
                        Bitmap.SetPixel(column, row, Color.FromArgb(
                            (int)(Buffer[BytesPerPixel * Columns * row + BytesPerPixel * column] -
                            (Buffer[BytesPerPixel * Columns * row + BytesPerPixel * column] % Math.Pow(2, 3))),
                            (int)((Buffer[BytesPerPixel * Columns * row + BytesPerPixel * column] % Math.Pow(2, 3)) * Math.Pow(2, 5) +
                            (Buffer[BytesPerPixel * Columns * row + BytesPerPixel * column + 1] -
                            (Buffer[BytesPerPixel * Columns * row + BytesPerPixel * column + 1] % Math.Pow(2, 5))) / Math.Pow(2, 3)),
                            (int)((Buffer[BytesPerPixel * Columns * row + BytesPerPixel * column + 1] % Math.Pow(2, 5)) * Math.Pow(2, 3))
                            ));
                    }
                }
                if (!IsFreezed) { pictureBox1.Image = (Image)Bitmap.Clone(); }
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image == null)
            {
                return;
            }

            var BitMapToSave = (Bitmap) pictureBox1.Image.Clone();

            Stream myStream;

            saveFileDialog1.Filter = "Png Image|*.png|JPeg Image|*.jpg|Bitmap Image|*.bmp|Gif Image|*.gif";
            saveFileDialog1.FilterIndex = 0;
            saveFileDialog1.RestoreDirectory = true;

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if ((myStream = saveFileDialog1.OpenFile()) != null)
                {
                    BitMapToSave.Save(myStream, ImageFormat.Png);

                    myStream.Close();
                }
            }
        }

        private void FreezeButton_Click(object sender, EventArgs e)
        {
            if (!IsFreezed)
            {
                FreezeButton.Text = "Freeze";
            }
            else
            {
                FreezeButton.Text = "Unfreeze";
            }
            IsFreezed = !IsFreezed;
        }

        private void SetAsComparisonImage(object sender, EventArgs e)
        {
            if (pictureBox1.Image == null)
            {
                pictureBox2.Image = null;
                return;
            }
            pictureBox2.Image = (Image)pictureBox1.Image.Clone();
        }

        private void SaveComparisonImage(object sender, EventArgs e)
        {
            if (pictureBox2.Image == null)
            {
                return;
            }

            var BitMapToSave = (Bitmap)pictureBox2.Image.Clone();

            Stream myStream;

            saveFileDialog1.Filter = "Png Image|*.png|JPeg Image|*.jpg|Bitmap Image|*.bmp|Gif Image|*.gif";
            saveFileDialog1.FilterIndex = 0;
            saveFileDialog1.RestoreDirectory = true;

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if ((myStream = saveFileDialog1.OpenFile()) != null)
                {
                    BitMapToSave.Save(myStream, ImageFormat.Png);

                    myStream.Close();
                }
            }
        }
    }
}
