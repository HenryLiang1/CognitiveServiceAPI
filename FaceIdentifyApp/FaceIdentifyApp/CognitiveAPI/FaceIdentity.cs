using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaceIdentifyApp
{
    public class FaceIdentity
    {

        //Emotion API key;
        private string faceApiKey = "eccd87633d4a48ac9afaf2113208a97c";
        private FaceServiceClient faceServiceClient;


        public FaceIdentity()
        {
            faceServiceClient = new FaceServiceClient(this.faceApiKey);
        }

        // get identify data
        public async Task<string> GetIdentity(Stream imageStream)
        {
            // Create an empty person group
            string personGroupId = "pev-user";
            string result = "No face";

            var faces = await faceServiceClient.DetectAsync(imageStream);

            if(faces.Length > 0)
            {
                var faceIds = faces.Select(face => face.FaceId).ToArray();
                Guid candidateId;
                Person person = null;
                

                var results = await faceServiceClient.IdentifyAsync(personGroupId, faceIds);
                foreach (var identifyResult in results)
                {
                    Debug.WriteLine("Result of face: {0}", identifyResult.FaceId);
                    if (identifyResult.Candidates.Length == 0)
                    {
                        Debug.WriteLine("No one identified");
                        //identityTextBox.Text = "No one identified";
                        //socketClient.Connect("LED0"); //white
                        result = "unidentified";
                    }
                    else
                    {
                        candidateId = identifyResult.Candidates[0].PersonId;
                        person = await faceServiceClient.GetPersonAsync(personGroupId, candidateId);
                        Debug.WriteLine("Identified as {0}", person.Name);
                        result = person.Name;
                    }
                }
            }
            

            return result;
        }
    }
}
