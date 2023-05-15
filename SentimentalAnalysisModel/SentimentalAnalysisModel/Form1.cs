using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.ML;
using Microsoft.ML.Data;
using System.Text;
using System.Linq;
using System.Diagnostics;

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
            var modelPath = "C:\\Users\\Administrator\\Desktop\\SentimentAnalysis\\SentimentalAnalysisModel\\SentimentalAnalysisModel\\SentimentModel.zip";
            var model = mlContext.Model.Load(modelPath, out var schema);

            // Create a prediction engine
            engine = mlContext.Model.CreatePredictionEngine<InputData, OutputData>(model);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void Open_Click(object sender, EventArgs e)
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
            float positive = 0, negative = 0, neutral = 0, notRelated = 0;
            var total = reviews.Count;
            var count = 0;

            StringBuilder reviewBuilder = new StringBuilder();
            foreach (var review in reviews)
            {
                reviewBuilder.Clear();
                reviewBuilder.Append(review);

                Debug.WriteLine($"{count++} Reviews Checked");
                var sampleData = new InputData
                {
                    Review = reviewBuilder.ToString()
                };

                var output = engine.Predict(sampleData);

                switch (output.PredictedLabel)
                {
                    case ("Positive"):
                        positive++;
                        break;
                    case ("Negative"):
                        negative++;
                        break;
                    case ("Neutral"):
                        neutral++;
                        break;
                    case ("Not Related"):
                        notRelated++;
                        break;
                }
                if (count == 100)
                    break;
            }

            float[] averages = { positive, negative, neutral, notRelated };

            averages = averages.Select(num => num / count).ToArray();

            check.Text = $"Positive: {positive} - Negative: {negative} - Neutral: {neutral} - Not Related: {notRelated}";
            result.Text = sortHelper.Sort(averages[0], averages[1], averages[2], averages[3]);
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
