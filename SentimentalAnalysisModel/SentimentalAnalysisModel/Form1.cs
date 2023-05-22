using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.ML;
using Microsoft.ML.Data;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Drawing;

namespace SentimentalAnalysisModel
{

    public partial class Form1 : Form
    {
        string path;
        List<string> reviews = new List<string>();
        private PredictionEngine<InputData, OutputData> engine;
        SortHelper sortHelper = new SortHelper();

        public Form1()
        {
            InitializeComponent();

            // Init Grid View
            dataGridView1.ColumnCount = 1;
            dataGridView1.Columns[0].Name = "Reviews";
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // AI Model
            var mlContext = new MLContext();
            var modelPath = "SentimentModel.zip";
            var model = mlContext.Model.Load(modelPath, out var schema);

            // Create a prediction engine
            engine = mlContext.Model.CreatePredictionEngine<InputData, OutputData>(model);

            button2.Enabled= false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            result.Text = "";
            check.Text = "";
            var sampleData = new InputData
            {
                Review = review.Text,
            };
            
            var output = engine.Predict(sampleData);

            result.Text = sortHelper.Sort(output.Score[0], output.Score[1], output.Score[2], output.Score[3]);
        }

        private void review_TextChanged(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            result.Text = "";
            check.Text = "";
            var total = reviews.Count;

            var progress = new ProgressBar(reviews);
            progress.ShowDialog();
            float[] averages = progress.Averages;

            check.Text = $"Positive: {averages[0]} - Negative: {averages[1]} - Neutral: {averages[2]} - Not Related: {averages[3]}";
            
            averages = averages.Select(num => num / total).ToArray();

            result.Text = sortHelper.Sort(averages[0], averages[1], averages[2], averages[3]);

            chart1.Series["Reviews"].Points.AddXY("Postive", averages[0]);
            chart1.Series["Reviews"].Points.AddXY("Negative", averages[1]);
            chart1.Series["Reviews"].Points.AddXY("Neutral", averages[2]);
            chart1.Series["Reviews"].Points.AddXY("Not Related", averages[3]);
        }

        private async void openToolStripMenuItem_ClickAsync(object sender, EventArgs e)
        {

            // Open File Code
            OpenFileDialog openFileDialog = new OpenFileDialog();

            // To list only csv files, we need to add this filter
            openFileDialog.Filter = "|*.csv";
            DialogResult result = openFileDialog.ShowDialog();

            if (result == DialogResult.OK)
            {
                path = openFileDialog.FileName;
                button2.Enabled = true;

            }
            else
            {
                return;
            }

            var messageBox = new MessageBox();
            messageBox.ShowDialog();

            // name and picture of the product
            string name = "", picUrl = "";

            using (TextFieldParser csvParser = new TextFieldParser(path))
            {
                int num = 1;
                csvParser.CommentTokens = new string[] { "#" };
                csvParser.SetDelimiters(new string[] { "," });
                csvParser.HasFieldsEnclosedInQuotes = true;

                // Skip the row with the column names
                csvParser.ReadLine();
                string[] fields;
                bool isParsed = false;
                while (!csvParser.EndOfData)
                {
                    // Read current line fields, pointer moves to the next line.
                    fields = csvParser.ReadFields();

                    // Access the first field (index 0) or other fields as needed
                    string fieldValue = fields[6];

                    if (!isParsed)
                    {
                        name = fields[2];
                        picUrl = fields[3];
                        isParsed = true;
                    }
                    // Remove the quotes if necessary
                    if (fieldValue.StartsWith("\"") && fieldValue.EndsWith("\""))
                    {
                        fieldValue = fieldValue.Trim('"');
                    }

                    dataGridView1.Rows.Add($"{num}.) {fieldValue}");
                    reviews.Add(fieldValue);
                    num++;
                }

            }

            label3.Text = name;

            // download image from url
            using (HttpClient httpClient = new HttpClient())
            {
                try
                {
                    // Download the image data as a byte array asynchronously
                    byte[] imageData = await httpClient.GetByteArrayAsync(picUrl);

                    // Create a MemoryStream from the byte array
                    using (var stream = new System.IO.MemoryStream(imageData))
                    {
                        // Create an Image object from the MemoryStream
                        Image image = Image.FromStream(stream);

                        // Set the Image object as the image in the PictureBox
                        pictureBox1.Image = image;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error downloading image: " + ex.Message);
                }
            }


            dataGridView1.Visible = true;
        }

        private void clearButton_Click(object sender, EventArgs e)
        {
            // Clear the chart
            chart1.Series["Reviews"].Points.Clear();

            // Clear the data grid
            dataGridView1.Rows.Clear();

            // Clear the picture box
            pictureBox1.Image = null;

            // Clear the label
            label3.Text = null;

            // Clear the reviews list
            reviews.Clear();

            // Clear the result and check labels
            result.Text = null;
            check.Text = null;

            button2.Enabled = false;
        }
    }

    public class InputData
    {
        [ColumnName(@"Review")]
        public string Review { get; set; }

        [ColumnName(@"Sentiment")]
        public string Sentiment { get; set; }
    }

    public class OutputData
    {
        [ColumnName(@"Review")]
        public float[] Review { get; set; }

        [ColumnName(@"Sentiment")]
        public uint Sentiment { get; set; }

        [ColumnName(@"Features")]
        public float[] Features { get; set; }

        [ColumnName(@"PredictedLabel")]
        public string PredictedLabel { get; set; }

        [ColumnName(@"Score")]
        public float[] Score { get; set; }
    }
}
