using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaceIdentifyApp
{
    class FaceIdentifyTraining
    {

        ////train face
        //private async void trainIdentity(Stream imageStream)
        //{
        //    // Create an empty person group
        //    string personGroupId = "pev-user";
        //    await faceServiceClient.CreatePersonGroupAsync(personGroupId, "My Friends");


        //    // Define Kent
        //    CreatePersonResult friend1 = await faceServiceClient.CreatePersonAsync(
        //        // Id of the person group that the person belonged to
        //        personGroupId,
        //        // Name of the person
        //        "Kent"
        //    );

        //    // Define Henry
        //    CreatePersonResult friend2 = await faceServiceClient.CreatePersonAsync(
        //        // Id of the person group that the person belonged to
        //        personGroupId,
        //        // Name of the person
        //        "Henry"
        //    );

        //    // Directory contains image files of Anna
        //    const string friend1ImageDir = @"C:\Users\Henry\Desktop\FaceRecognitionWpf\Kent";

        //    foreach (string imagePath in Directory.GetFiles(friend1ImageDir, "*.jpg"))
        //    {
        //        using (Stream s = File.OpenRead(imagePath))
        //        {
        //            // Detect faces in the image and add to Anna
        //            await faceServiceClient.AddPersonFaceAsync(
        //                personGroupId, friend1.PersonId, s);
        //        }
        //    }

        //    const string friend2ImageDir = @"C:\Users\Henry\Desktop\FaceRecognitionWpf\Henry";

        //    foreach (string imagePath in Directory.GetFiles(friend2ImageDir, "*.jpg"))
        //    {
        //        using (Stream s = File.OpenRead(imagePath))
        //        {
        //            // Detect faces in the image and add to Anna
        //            await faceServiceClient.AddPersonFaceAsync(
        //                personGroupId, friend2.PersonId, s);
        //        }
        //    }

        //    await faceServiceClient.TrainPersonGroupAsync(personGroupId);

        //    TrainingStatus trainingStatus = null;
        //    while (true)
        //    {
        //        trainingStatus = await faceServiceClient.GetPersonGroupTrainingStatusAsync(personGroupId);

        //        if (trainingStatus.Status.ToString() != "running")
        //        {
        //            break;
        //        }

        //        await Task.Delay(1000);
        //    }
        //}

    }
}
