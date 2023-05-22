using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.ML;
using Microsoft.ML.Data;
using System.Linq;
using System.Net.Http;
using System.Drawing;
using System.Net;
using HtmlAgilityPack;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO;

namespace SentimentalAnalysisModel
{

    public partial class Form1 : Form
    {
        string path;
        List<string> reviews = new List<string>();
        private PredictionEngine<InputData, OutputData> engine;
        SortHelper sortHelper = new SortHelper();
        private string baseUrl;
        private int currentPage;
        int num = 0;

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

            urlTextBox.Text = null;
            num = 0;
        }

        private void scrapeButton_Click(object sender, EventArgs e)
        {
            string url = urlTextBox.Text;
            if (!string.IsNullOrWhiteSpace(url))
            {
                try
                {
                    baseUrl = url;
                    currentPage = 1;

                    ScrapeReviews();
                }
                catch (Exception ex)
                {
                    // Handle any exceptions that may occur during the scraping process
                    Debug.WriteLine("An error occurred: " + ex.Message);
                }
            }
            else
            {
                Debug.WriteLine("Please enter a valid URL.");
            }
        }
        private void ScrapeReviews()
        {
            try
            {
                // Create the URL for the current page
                string url = $"{baseUrl}?pageNumber={currentPage}";

                // Create a WebClient to download the HTML content of the page
                WebClient client = new WebClient();
                string html = client.DownloadString(url);

                // Use HtmlAgilityPack to parse the HTML
                HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(html);

                if(num == 0)
                {
                    // Find the product image container on the page
                    HtmlNode productImageContainer = doc.DocumentNode.SelectSingleNode("//a[@class='a-link-normal']//img[@data-hook='cr-product-image']");
                    if (productImageContainer != null)
                    {
                        // Get the source URL of the image
                        string imageUrl = WebUtility.HtmlDecode(productImageContainer.GetAttributeValue("src", ""));

                        // Download the image using WebClient
                        WebClient imageClient = new WebClient();
                        byte[] imageData = imageClient.DownloadData(imageUrl);

                        // Create a MemoryStream from the image data
                        MemoryStream imageStream = new MemoryStream(imageData);

                        // Set the image in the PictureBox
                        pictureBox1.Image = Image.FromStream(imageStream);
                    }

                    // Find the product name container on the page
                    HtmlNode productNameContainer = doc.DocumentNode.SelectSingleNode("//a[@data-hook='product-link']");
                    if (productNameContainer != null)
                    {
                        // Get the product name
                        string productName = productNameContainer.InnerText.Trim();

                        // Use the product name as needed
                        label3.Text = productName;
                    }
                }     

                // Find the review containers on the page (adjust the XPath to match the specific structure of Amazon's page)
                HtmlNode reviewContainer = doc.DocumentNode.SelectSingleNode("//div[@id='cm_cr-review_list']");

                if (reviewContainer != null)
                {
                    // Find all the comment nodes within the review container
                    HtmlNodeCollection commentNodes = reviewContainer.SelectNodes(".//div[@class='a-row a-spacing-small review-data']//span[@data-hook='review-body']");
                    button2.Enabled = true;

                    if (commentNodes != null)
                    {

                        // Extract the comment text from each node
                        foreach (HtmlNode commentNode in commentNodes)
                        {
                            //extractedData += commentNode.InnerText.Trim() + Environment.NewLine + Environment.NewLine;
                            if (!commentNode.InnerText.Contains("The media could not be loaded."))
                            {
                                string commentText = commentNode.InnerText.Trim();
                                commentText = FilterNonTextCharacters(commentText);
                                dataGridView1.Rows.Add($"{num + 1}.) {commentText}");
                                reviews.Add(commentText);
                                Debug.WriteLine(commentText + "\n");
                                num++;

                                int limit = 0;
                                Int32.TryParse(dataCount.Text, out limit);
                                if (num == limit)
                                    return;
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine("No comments found on the page.");
                    }
                    // Check if there is a next page
                    HtmlNode nextPageListItem = doc.DocumentNode.SelectSingleNode("//li[@class='a-last']");
                    if (nextPageListItem != null)
                    {
                        HtmlNode nextPageLink = nextPageListItem.SelectSingleNode(".//a");
                        Debug.WriteLine("Next page: ");
                        if (nextPageLink != null)
                        {
                            // Get the URL of the next page
                            string nextPageUrl = WebUtility.HtmlDecode(nextPageLink.GetAttributeValue("href", ""));

                            // Remove any query parameters from the URL
                            nextPageUrl = nextPageUrl.Split('?')[0];

                            // Create the complete URL for the next page
                            nextPageUrl = new Uri(new Uri(baseUrl), nextPageUrl).ToString();

                            // Scrape the next page
                            currentPage++;
                            baseUrl = nextPageUrl;
                            ScrapeReviews();
                        }
                    }
                }
                else
                {
                    Debug.WriteLine("No review container found on the page.");
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions that may occur during the scraping process
                Debug.WriteLine("An error occurred: " + ex.Message);
            }
        }
        private string FilterNonTextCharacters(string text)
        {
            // Use a regular expression to filter out non-alphabetic letters and digits
            return Regex.Replace(text, @"[^a-zA-Z0-9!&'?.,%+:=]+", " ");
        }

        private void check_Click(object sender, EventArgs e)
        {

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
