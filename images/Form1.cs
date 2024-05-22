using System;
using System.Drawing;
using System.Windows.Forms;
using AForge.Imaging.Filters;
using Accord.Imaging;
using Accord.Imaging.Filters;
using Accord.Math;
using NAudio.Wave;


namespace image
{
    public partial class Form1 : Form
    {
        private WaveInEvent waveIn;
        private WaveFileWriter writer;
        private bool isRecording = false;
        private string outputFilePath = "C:\\Users\\EUROPELAPTOP\\Desktop\\multimedia\\images.wav"; // Change to your desired path

        public Form1()
        {
            InitializeComponent();
        }

        private void UploadImageButton1_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif";
                openFileDialog.Title = "Select an Image";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    pictureBox1.Image = System.Drawing.Image.FromFile(openFileDialog.FileName);
                }
            }
        }

        private void UploadImageButton2_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif";
                openFileDialog.Title = "Select an Image";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    pictureBox2.Image = System.Drawing.Image.FromFile(openFileDialog.FileName);
                }
            }
        }

        private void CompareButton_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image == null || pictureBox2.Image == null)
            {
                MessageBox.Show("Please upload both images before comparing.", "Comparison Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Convert PictureBox images to Bitmap
            Bitmap image1 = new Bitmap(pictureBox1.Image);
            Bitmap image2 = new Bitmap(pictureBox2.Image);

            // Add text comment to image1
            string comment = textBoxComment.Text;
            if (!string.IsNullOrWhiteSpace(comment))
            {
                image1 = DrawTextOnImage(image1, comment, new Point(10, 10)); // Adjust position as needed
            }

            // Resize images to the same size if they are different
            if (image1.Width != image2.Width || image1.Height != image2.Height)
            {
                Bitmap resizedImage2 = new Bitmap(image1.Width, image1.Height);
                using (Graphics g = Graphics.FromImage(resizedImage2))
                {
                    g.DrawImage(image2, new Rectangle(0, 0, resizedImage2.Width, resizedImage2.Height));
                }
                image2 = resizedImage2;
            }

            // Convert to grayscale using Accord.Imaging.Filters.Grayscale
            Accord.Imaging.Filters.Grayscale grayscaleFilter = new Accord.Imaging.Filters.Grayscale(0.2125, 0.7154, 0.0721);
            Bitmap grayImage1 = grayscaleFilter.Apply(image1);
            Bitmap grayImage2 = grayscaleFilter.Apply(image2);

            // Calculate SSIM (Structural Similarity Index)
            double ssim = CalculateSSIM(grayImage1, grayImage2);

            // Define thresholds for changes
            double noChangeThreshold = 0.98;
            double improvementThreshold = 0.85;

            // Determine progress based on the SSIM
            string result;
            if (ssim > noChangeThreshold)
            {
                result = "The images are similar. There is no significant progress.";
            }
            else if (ssim <= noChangeThreshold && ssim > improvementThreshold)
            {
                result = "There is some progress in the treatment.";
            }
            else
            {
                result = "There is progress in the illness.";
            }

            
        // Show the result
        MessageBox.Show(result, "Comparison Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private double CalculateSSIM(Bitmap img1, Bitmap img2)
        {
            // Convert images to Accord's format
            double[,] img1Array = ConvertToDoubleArray(img1);
            double[,] img2Array = ConvertToDoubleArray(img2);

            // Calculate SSIM
            return SSIM(img1Array, img2Array);
        }

        private double[,] ConvertToDoubleArray(Bitmap img)
        {
            // Convert Bitmap to double[,] array
            int width = img.Width;
            int height = img.Height;
            double[,] result = new double[height, width];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color pixel = img.GetPixel(x, y);
                    result[y, x] = pixel.R / 255.0; // Assuming grayscale image
                }
            }

            return result;
        }

        private double SSIM(double[,] img1, double[,] img2)
        {
            // Implement SSIM calculation here
            // Constants
            double K1 = 0.01;
            double K2 = 0.03;
            double L = 1.0; // Dynamic range of the pixel values

            // Mean values
            double mean1 = Mean(img1);
            double mean2 = Mean(img2);

            // Variance and covariance
            double variance1 = Variance(img1, mean1);
            double variance2 = Variance(img2, mean2);
            double covariance = Covariance(img1, img2, mean1, mean2);

            // SSIM calculation
            double C1 = Math.Pow(K1 * L, 2);
            double C2 = Math.Pow(K2 * L, 2);

            double ssim = (2 * mean1 * mean2 + C1) * (2 * covariance + C2) /
                          ((mean1 * mean1 + mean2 * mean2 + C1) * (variance1 + variance2 + C2));

            return ssim;
        }

        private double Mean(double[,] img)
        {
            double sum = 0.0;
            int count = img.Length;

            foreach (double value in img)
            {
                sum += value;
            }

            return sum / count;
        }

        private double Variance(double[,] img, double mean)
        {
            double sum = 0.0;
            int count = img.Length;

            foreach (double value in img)
            {
                sum += Math.Pow(value - mean, 2);
            }

            return sum / count;
        }

        private double Covariance(double[,] img1, double[,] img2, double mean1, double mean2)
        {
            double sum = 0.0;
            int height = img1.GetLength(0);
            int width = img1.GetLength(1);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    sum += (img1[y, x] - mean1) * (img2[y, x] - mean2);
                }
            }

            return sum / img1.Length;
        }
        private void RecordButton_Click(object sender, EventArgs e)
        {
            if (!isRecording)
            {
                StartRecording();
                recordButton.Text = "Stop Recording";
            }
            else
            {
                StopRecording();
                recordButton.Text = "Record Comment";
            }
        }

        private void StartRecording()
        {
            waveIn = new WaveInEvent();
            waveIn.WaveFormat = new WaveFormat(44100, 1);
            waveIn.DataAvailable += OnDataAvailable;
            writer = new WaveFileWriter(outputFilePath, waveIn.WaveFormat);

            waveIn.StartRecording();
            isRecording = true;
        }

        private void StopRecording()
        {
            waveIn.StopRecording();
            waveIn.Dispose();
            writer.Close();
            writer.Dispose();
            isRecording = false;

            MessageBox.Show($"Recording saved to: {outputFilePath}", "Recording Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            if (writer != null)
            {
                writer.Write(e.Buffer, 0, e.BytesRecorded);
                writer.Flush();
            }
        }
        private Bitmap DrawTextOnImage(Bitmap image, string text, Point position)
        {
            Bitmap newImage = new Bitmap(image);
            using (Graphics g = Graphics.FromImage(newImage))
            {
                using (Font arialFont = new Font("Arial", 20))
                {
                    g.DrawString(text, arialFont, Brushes.Black, position);
                }
            }
            return newImage;
        }
        private void SaveTextButton_Click(object sender, EventArgs e)
        {
            if (radioButtonImage1.Checked && pictureBox1.Image != null)
            {
                AddTextToImageAndSave(pictureBox1, textBoxComment.Text);
            }
            else if (radioButtonImage2.Checked && pictureBox2.Image != null)
            {
                AddTextToImageAndSave(pictureBox2, textBoxComment.Text);
            }
            else
            {
                MessageBox.Show("Please select an image and enter a comment.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void AddTextToImageAndSave(PictureBox pictureBox, string text)
        {
            Bitmap image = new Bitmap(pictureBox.Image);
            image = DrawTextOnImage(image, text, new Point(10, 10)); // You can adjust the position

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "JPEG Image|*.jpg|Bitmap Image|*.bmp|PNG Image|*.png",
                Title = "Save an Image File",
                FileName = "ImageWithText"
            };

            if (saveFileDialog.ShowDialog() == DialogResult.OK && saveFileDialog.FileName != "")
            {
                image.Save(saveFileDialog.FileName);
            }

            pictureBox.Image = image; // Update PictureBox with new image
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {

        }
    }
}
