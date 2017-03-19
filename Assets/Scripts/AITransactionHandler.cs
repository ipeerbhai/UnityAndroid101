using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using UnityEngine;

namespace Assets.Scripts
{
    public class AITransactionHandler
    {
        public string AuthorizationString = "SMe75ff0bdf59cdc3c7106aaad84e7f16a";
        private string m_BaseURI = @"http://dev.thinkpredict.com:3000";
        //private string m_BaseURI = @"http://127.0.0.1:3000";

        public static bool HasNewModel { get; internal set; }

        //----------------------------------------------------------------------------------------------
        public void SendDataToCloud(MemoryStream waveData, string relativeEndpoint, string _contentType)
        {
            // We need to make an HTTP Request to a URI and send the chunked wavstream.
            string requestUri = m_BaseURI + relativeEndpoint;
            string responseString = "";
            string contentType = _contentType;

            SceneManager.messageToDisplay += "Contacting cloud";

            // setup an http request, transfer the memorystream.
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(requestUri);
            httpWebRequest.ContentType = contentType;
            httpWebRequest.SendChunked = true;
            httpWebRequest.Accept = @"text/plain";
            httpWebRequest.Method = "POST";
            httpWebRequest.Headers["Authorization"] = AuthorizationString;
            httpWebRequest.ProtocolVersion = HttpVersion.Version11;

            // make the request, stream the chunks.
            SceneManager.messageToDisplay = "making request";
            try
            {
                /*
                 * Open a request stream and write 1024 byte chunks in the stream one at a time.
                 */

                byte[] buffer = null;
                int bytesRead = 0;
                waveData.Position = 0;
                using (Stream requestStream = httpWebRequest.GetRequestStream())
                {
                    /*
                     * Read 1024 raw bytes from the input audio file.
                     */
                    buffer = new Byte[checked((uint)Math.Min(1024, (int)waveData.Length))];
                    while ((bytesRead = waveData.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        requestStream.Write(buffer, 0, bytesRead);
                    }

                    // Flush
                    requestStream.Flush();
                }

                /*
                 * Get the response from the service.
                 */
                SceneManager.messageToDisplay = "getting response";
                using (WebResponse response = httpWebRequest.GetResponse())
                {
                    string responseContent = response.ContentType;

                    Stream ReceiveStream = response.GetResponseStream();
                    Encoding encode = System.Text.Encoding.GetEncoding("utf-8");

                    // Pipe the stream to a higher level stream reader with the required encoding format. 
                    StreamReader readStream = new StreamReader(ReceiveStream, encode);
                    Char[] read = new Char[256];

                    // Read 256 charcters at a time.    
                    int count = readStream.Read(read, 0, 256);
                    while (count > 0)
                    {
                        // Dump the 256 characters on a string and display the string onto the console.
                        String str = new String(read, 0, count);
                        //Console.Write(str);
                        responseString += str;
                        count = readStream.Read(read, 0, 256);
                    }

                    // close the readStream.
                    readStream.Close();
                    response.Close();
                }
            } // end try
            catch (WebException ex)
            {
                SceneManager.messageToDisplay += "Error, could not contact cloud.";
                SceneManager.messageToDisplay += ex.Message;
            }

            if (responseString.Length > 2)
                SceneManager.messageToDisplay += responseString;
            else
                SceneManager.messageToDisplay += "Error, could not contact cloud.";


            string FileName = @"c:\temp\GvrTest.wav";
            waveData.Position = 0;
            FileStream FSWriteMe;
            FSWriteMe = new FileStream(FileName, FileMode.Create);
            FSWriteMe.Write(waveData.ToArray(), 0, (int)waveData.Length);
            FSWriteMe.Flush();
            FSWriteMe.Dispose();

            HasNewModel = true;

            return;
        }

        //----------------------------------------------------------------------------------------------
        public string GetTextDataFromCloud(string relativeEndpoint)
        {
            string result;
            string requestUri = m_BaseURI + relativeEndpoint;

            var httpWebRequest = (HttpWebRequest)WebRequest.Create(requestUri);
            httpWebRequest.ContentType = @"text/plain";
            httpWebRequest.Method = "POST";
            httpWebRequest.Headers["Authorization"] = AuthorizationString;

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                result = streamReader.ReadToEnd(); // this only works if we don't previously drain the stream.
            }
            return (result);
        }

        //----------------------------------------------------------------------------------------------
        public void SendDataToCloud(MemoryStream waveData)
        {
            SendDataToCloud(waveData, "/v1/PostWave", @"audio/x-wav;codec=pcm;bit=16;rate=16000");
        }

        //----------------------------------------------------------------------------------------------
        // for now, we'll pretend that we got a valid model from the controller.
        static public List<GameObject> GetFullModelFromCloud()
        {
            List<GameObject> fullModel = new List<GameObject>();
            AITransactionHandler me = new AITransactionHandler();

            // let's get the model from the cloud and parse it into gameobjects.
            string stringModel = me.GetTextDataFromCloud("/v1/GetModel");
            string[] modelLines = stringModel.Split('\n');
            foreach (string line in modelLines)
            {
                GameObject Parsed = ParseModel(line);
                if (Parsed != null)
                    fullModel.Add(Parsed);
            }
            HasNewModel = false;
            return (fullModel);
        }

        //----------------------------------------------------------------------------------------------
        private static GameObject ParseModel(string line)
        {
            GameObject output = null;
            if ((line != null) && (line.Length > 2))
            {
                string[] goDefinition = line.Split(',');
                // put the schema in here.
                if (goDefinition.Count() == 13)
                {
                    string Name = goDefinition[0];
                    bool AllowedRotateX = bool.Parse(goDefinition[1]);
                    bool AllowedRotateY = bool.Parse(goDefinition[2]);
                    bool AllowedRotateZ = bool.Parse(goDefinition[3]);
                    float PositionX = float.Parse(goDefinition[4]);
                    float PositionY = float.Parse(goDefinition[5]);
                    float PositionZ = float.Parse(goDefinition[6]);
                    float RotationAroundX = float.Parse(goDefinition[7]);
                    float RotationAroundY = float.Parse(goDefinition[8]);
                    float RotationAroundZ = float.Parse(goDefinition[9]);
                    float ScaleX = float.Parse(goDefinition[10]);
                    float ScaleY = float.Parse(goDefinition[11]);
                    float ScaleZ = float.Parse(goDefinition[12]);

                    // build the gameobject.
                    if (Name.ToLowerInvariant().Contains("wall"))
                    {
                        output = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    }
                    if (output != null)
                    {
                        output.transform.localScale = new Vector3(ScaleX, ScaleY, ScaleZ);
                        output.transform.position = new Vector3(PositionX, PositionY, PositionZ);
                        if (AllowedRotateX)
                            output.transform.Rotate(Vector3.right, RotationAroundX);
                        if (AllowedRotateY)
                            output.transform.Rotate(Vector3.up, RotationAroundY);
                        if (AllowedRotateZ)
                            output.transform.Rotate(Vector3.forward, RotationAroundZ);
                    }
                }
            }
            return (output);
        }
    }
}
