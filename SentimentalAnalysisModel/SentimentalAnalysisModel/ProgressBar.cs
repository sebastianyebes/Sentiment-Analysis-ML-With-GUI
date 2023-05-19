using Microsoft.ML;
using Microsoft.VisualBasic.Devices;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace SentimentalAnalysisModel
{
    public partial class ProgressBar : Form
    {

        private PredictionEngine<InputData, OutputData> engine;
        private List<string> reviews = new List<string>();
        private float[] averages;

        public float[] Averages { get { return averages; } }
        public float Positive { get; set; } = 100;

        public ProgressBar(List<string> reviews)
        {
            InitializeComponent();
            progressBar1.Value = 0;

            // AI Model
            var mlContext = new MLContext();
            var modelPath = "SentimentModel.zip";
            var model = mlContext.Model.Load(modelPath, out var schema);

            // Create a prediction engine
            engine = mlContext.Model.CreatePredictionEngine<InputData, OutputData>(model);
            this.reviews = reviews;

            Start();
        }


        struct DataParameter
        {
            public int Process;
            public int Delay;
        }

        private DataParameter _inputParameter;

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            int process = ((DataParameter)e.Argument).Process;
            int delay = ((DataParameter)e.Argument).Delay;
            int total = reviews.Count;

            try
            {
                float positive = 0, negative = 0, neutral = 0, notRelated = 0;
                int count = 0;

                foreach (var review in reviews)
                {
                    if (!backgroundWorker1.CancellationPending)
                    {
                        // Calculate progress based on current count and total reviews
                        int progress = (int)((count + 1) / (float)total * 100);
                        backgroundWorker1.ReportProgress(progress, $"Processing data {count + 1} of {total}");
                        Thread.Sleep(delay);

                        var sampleData = new InputData
                        {
                            Review = review
                        };

                        var output = engine.Predict(sampleData);

                        switch (output.PredictedLabel)
                        {
                            case "Positive":
                                positive++;
                                break;
                            case "Negative":
                                negative++;
                                break;
                            case "Neutral":
                                neutral++;
                                break;
                            case "Not Related":
                                notRelated++;
                                break;
                        }

                        count++;
                    }
                    else
                    {
                        e.Cancel = true;
                        return;
                    }
                }

                averages = new float[] { positive, negative, neutral, notRelated };
            }
            catch (Exception ex)
            {
                backgroundWorker1.CancelAsync();
                MessageBox.Show(ex.Message, "Message", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
            label1.Text = $"Processing....{e.ProgressPercentage}%";
            progressBar1.Update();
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.Close();
        }

        private void Start()
        {
            if (!backgroundWorker1.IsBusy)
            {
                _inputParameter.Delay = 5;
                _inputParameter.Process = 10;
                backgroundWorker1.RunWorkerAsync(_inputParameter);
            }
        }
    }
}
