using Microsoft.ProjectOxford.Emotion;
using Microsoft.ProjectOxford.Emotion.Contract;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace FaceIdentifyApp
{
    public class EmotionRecognition
    {
        private List<EmotionScore> emotionScoresList;
        private float maxEmotionScore;
        private EmotionScore likelyEmotion;
        private EmotionServiceClient emotionServiceClient;

        //Emotion API key;
        private string emotionApiKey = "6927a72d4c7248f6a7769144e17dc695";

        //todo
        public event UpdateScreenEventHandler UpdateScreenEvent;
        public delegate void UpdateScreenEventHandler(string emotionResult);

        public EmotionRecognition()
        {
            emotionServiceClient = new EmotionServiceClient(this.emotionApiKey);
            emotionScoresList = new List<EmotionScore>();
        }
        
        //get emotion data
        public async Task<string> GetEmotions(Stream imageStream)
        {

            emotionScoresList.Clear();// clear the previous emotional result
            Emotion[] emotionResult = await emotionServiceClient.RecognizeAsync(imageStream);

            //emotionResult.
            //var firstEmotion = emotionResult.First();

            var sb1 = new StringBuilder();
            var sb2 = new StringBuilder();
            var faceNumber = 0;
            foreach (Emotion em in emotionResult)
            {
                faceNumber++;
                var scores = em.Scores;

                var anger = scores.Anger;
                var contempt = scores.Contempt;
                var disgust = scores.Disgust;
                var fear = scores.Fear;
                var happiness = scores.Happiness;
                var neutral = scores.Neutral;
                var surprise = scores.Surprise;
                var sadness = scores.Sadness;

                emotionScoresList.Add(new EmotionScore("anger", anger));
                emotionScoresList.Add(new EmotionScore("contempt", contempt));
                emotionScoresList.Add(new EmotionScore("disgust", disgust));
                emotionScoresList.Add(new EmotionScore("fear", fear));
                emotionScoresList.Add(new EmotionScore("happiness", happiness));
                emotionScoresList.Add(new EmotionScore("neutral", neutral));
                emotionScoresList.Add(new EmotionScore("surprise", surprise));
                emotionScoresList.Add(new EmotionScore("sadness", sadness));

                maxEmotionScore = emotionScoresList.Max(e => e.EmotionValue);
                likelyEmotion = emotionScoresList.First(e => e.EmotionValue == maxEmotionScore);

                Debug.WriteLine("likelyEmotionTYPE" + likelyEmotion.GetType());
                Debug.WriteLine("likelyEmotion" + likelyEmotion.EmotionName);
            }
            return likelyEmotion.EmotionName;
        }
    }

    //emotion data structure
    public class EmotionScore
    {
        public string EmotionName
        {
            get;
            set;
        }
        public float EmotionValue
        {
            get;
            set;
        }

        public EmotionScore(string emotionName, float emotionValue)
        {
            EmotionName = emotionName;
            EmotionValue = emotionValue;
        }


        public override string ToString()
        {
            return this.EmotionName + ": " + this.EmotionValue;
        }
    }
}
