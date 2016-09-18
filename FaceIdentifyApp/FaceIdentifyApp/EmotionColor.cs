using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.SpeechSynthesis;
using Windows.UI.Xaml.Controls;

namespace FaceIdentifyApp
{
    public class EmotionColor
    {
        private SocketClient socketClient;
        
        // The object for controlling the speech synthesis engine (voice).
        private SpeechSynthesizer speechSynthesizer;
       
        // The media object for controlling and playing audio.
        private MediaElement mediaElement;
        private string colorCommand;

        public EmotionColor()
        {
            socketClient = new SocketClient();
            speechSynthesizer = new SpeechSynthesizer();
            mediaElement = new MediaElement();
        }

        public async void SetColor(string identityResult, string emotion)
        {
            SpeechSynthesisStream voiceStream = null;
            

            if (identityResult == "unidentified")
            {
                colorCommand = "LED0";
                voiceStream = await speechSynthesizer.SynthesizeTextToStreamAsync("I don't know who you are");

            }
            else
            {
                switch (emotion)
                {
                    case "happiness":
                        colorCommand = "RGB0 255 0"; //Green
                        voiceStream = await speechSynthesizer.SynthesizeTextToStreamAsync("Hi " + identityResult +", you look happy!!!");
                        break;
                    case "neutral":
                        colorCommand = "RGB255 255 255"; //White
                        voiceStream = await speechSynthesizer.SynthesizeTextToStreamAsync("Hi " + identityResult + ", you look fine!!!");
                        break;
                    case "anger":
                        colorCommand = "RGB255 0 0"; //Red
                        voiceStream = await speechSynthesizer.SynthesizeTextToStreamAsync("Hi " + identityResult + ", you look angry!!!");
                        break;
                    case "sadness":
                        colorCommand = "RGB0 0 255"; //Blue
                        voiceStream = await speechSynthesizer.SynthesizeTextToStreamAsync("Hi " + identityResult + ", you look sad!!!");
                        break;
                    case "surprise":
                        colorCommand = "RGB255 255 0"; //Yellow
                        voiceStream = await speechSynthesizer.SynthesizeTextToStreamAsync("Hi " + identityResult + ", you look surprise!!!");
                        break;
                    case "fear":
                        colorCommand = "RGB255 165 0"; //Orange
                        voiceStream = await speechSynthesizer.SynthesizeTextToStreamAsync("Hi " + identityResult + ", you look scary!!!");
                        break;
                    case "disgust":
                        colorCommand = "RGB128 0 128"; //Orange
                        voiceStream = await speechSynthesizer.SynthesizeTextToStreamAsync("Hi " + identityResult + ", you look disgusting!!!");
                        break;
                    case "contempt":
                        colorCommand = "RGB255 0 255"; //Magenta
                        voiceStream = await speechSynthesizer.SynthesizeTextToStreamAsync("Hi " + identityResult + ", you look contemptuous!!!");
                        break;
                }
            }
            socketClient.Connect(colorCommand);
            // Send the stream to the media object.
            mediaElement.SetSource(voiceStream, voiceStream.ContentType);
            mediaElement.Play();

        }


    }
}
