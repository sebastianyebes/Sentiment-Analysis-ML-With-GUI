using System;
using System.Collections.Generic;
using System.Linq;

namespace SentimentalAnalysisModel
{
    public class SortHelper
    {
        public string Sort(float positive, float negative, float neutral, float notRelated)
        {
            List<float> init = new List<float> { positive, negative, neutral, notRelated };
            int i = 0;
            init.Sort();
            init.Reverse();
            string result = "";

            while (i < 4)
            {
                if (positive == init[i])
                {
                    result += $"Positive: {Math.Round(positive * 100, 0)}%\n";
                }
                else if (negative == init[i])
                {
                    result += $"Negative: {Math.Round(negative * 100, 0)}%\n";
                }
                else if (neutral == init[i])
                {
                    result += $"Neutral: {Math.Round(neutral * 100, 0)}%\n";
                }
                else if (notRelated == init[i])
                {
                    result += $"Not Related: {Math.Round(notRelated * 100, 0)}%\n";
                }
                i++;
            }

            return result;
        }
    }
}
