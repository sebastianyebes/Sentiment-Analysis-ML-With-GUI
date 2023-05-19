using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.ML;
using Microsoft.ML.Data;
using System.Linq;

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

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Open File Code
            OpenFileDialog openFileDialog = new OpenFileDialog();

            // To list only csv files, we need to add this filter
            openFileDialog.Filter = "|*.csv";
            DialogResult result = openFileDialog.ShowDialog();

            if (result == DialogResult.OK)
            {
                path = openFileDialog.FileName;

            }
            else
            {
                return;
            }

            var messageBox = new MessageBox();
            messageBox.ShowDialog();

            using (TextFieldParser csvParser = new TextFieldParser(path))
            {
                int num = 1;
                csvParser.CommentTokens = new string[] { "#" };
                csvParser.SetDelimiters(new string[] { "," });
                csvParser.HasFieldsEnclosedInQuotes = true;

                // Skip the row with the column names
                csvParser.ReadLine();

                while (!csvParser.EndOfData)
                {
                    // Read current line fields, pointer moves to the next line.
                    string[] fields = csvParser.ReadFields();
                    dataGridView1.Rows.Add($"{num}.) {fields[0]}");
                    reviews.Add(fields[0]);
                    num++;
                }
            }

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
