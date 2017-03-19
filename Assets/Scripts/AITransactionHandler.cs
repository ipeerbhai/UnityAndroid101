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
        public string AccessCode = "SMe75ff0bdf59cdc3c7106aaad84e7f16a";
        private string m_BaseURI = @"http://dev.thinkpredict.com:3000";

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
            httpWebRequest.Headers["Authorization"] = AccessCode;
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
            return;
        }


        //----------------------------------------------------------------------------------------------
        public void SendDataToCloud(MemoryStream waveData)
        {
            SendDataToCloud(waveData, "/v1/PostWave", @"audio/x-wav;codec=pcm;bit=16;rate=16000");
        }

        //----------------------------------------------------------------------------------------------
        public List<GameObject> GetFullModelFromCloud()
        {
            throw new NotImplementedException();
        }
    }
}
