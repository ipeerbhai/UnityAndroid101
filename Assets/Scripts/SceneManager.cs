using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SceneManager : MonoBehaviour
{
    private Vector2 touchOrigin = -Vector2.one;
    private float m_forwardSpeed = 3.0f;

    private AudioSource microphoneAudioSource = null;
    private SoundClipManager soundMgr;
    private int secondsToRecord = 10;
    private float m_timeAccumulator = 0;
    static public string messageToDisplay { get; set; }
    private static List<GameObject> model;

    public void DisplayMessage(string message = "")
    {
        m_timeAccumulator += Time.deltaTime;
        if (m_timeAccumulator > 0.8f)
        {
            if (messageToDisplay.Contains("\n"))
            {
                int newlinePos = messageToDisplay.IndexOf('\n') + 1;
                if (newlinePos < messageToDisplay.Length - 1)
                    messageToDisplay = messageToDisplay.Substring(newlinePos);
            }
            messageToDisplay += "\n";
            m_timeAccumulator = 0;
        }
        if (messageToDisplay.Length > 100)
            messageToDisplay = (message.Length > 3) ? message + "\n" : messageToDisplay;
        else
            messageToDisplay += message;
        var theTextGameObject = GameObject.Find("txtMainData");
        UnityEngine.UI.Text theTextComponent = theTextGameObject.GetComponent<UnityEngine.UI.Text>();
        theTextComponent.text = messageToDisplay;
    }

    public void DestroyOrInstantiateModel(bool DestroyMode = true)
    {
        if ((model != null) && (model.Count > 0))
        {
            foreach (GameObject thisGO in model)
            {
                if (DestroyMode)
                    Destroy(thisGO);
                else
                    Instantiate(thisGO);
            }
            if (DestroyMode)
                model = null;
        }
    }

    // Use this for initialization
    void Start()
    {
        model = new List<GameObject>() { GameObject.CreatePrimitive(PrimitiveType.Capsule) };
        messageToDisplay = "";
        DisplayMessage("Start Success");
        microphoneAudioSource = GetComponent<AudioSource>();
        soundMgr = new SoundClipManager();
    }

    // Update is called once per frame
    void Update()
    {
        DisplayMessage();
        // get the player object, as we often do things with it...
        var player = GameObject.Find("Player");
        float deadZone = 0.15f; // used to change speed of the player.

        bool bStartSomething = false;
        bool bEndSomething = false;

#if UNITY_HAS_GOOGLEVR && (UNITY_ANDROID || UNITY_EDITOR)
        if (GvrController.AppButtonDown)
            bStartSomething = true;

        if (GvrController.AppButtonUp)
            bEndSomething = true;
#endif
        if (Input.GetKeyDown(KeyCode.Space))
            bStartSomething = true;

        if (Input.GetKeyUp(KeyCode.Space))
            bEndSomething = true;

        if (Input.touchCount > 0)
        {
            Touch myTouch = Input.touches[0];
            if (myTouch.phase == TouchPhase.Began)
            {
                touchOrigin = myTouch.position;
                bStartSomething = true;
            }
            if (myTouch.phase == TouchPhase.Ended)
            {
                bEndSomething = true;
            }
        }

        if (bStartSomething)
        {
            if (!Microphone.IsRecording(null))
            {
                DisplayMessage("Starting recording");
                soundMgr.Clear();
                soundMgr.ClipStart();
                microphoneAudioSource.clip = Microphone.Start(null, true, secondsToRecord, 16000);
                microphoneAudioSource.loop = true;
            }
        }

        float recordingPosition = -1;
        if (Microphone.IsRecording(null))
        {
            recordingPosition = (Microphone.GetPosition(null) / 16000) * secondsToRecord;

            // if the Mic is getting close to the buffer length, consolidate the clip, stop, and restart the Mic.
            if (recordingPosition > secondsToRecord - 1)
            {
                int numSamples = Microphone.GetPosition(null);
                Microphone.End(null);
                DisplayMessage("Buffer full");
                soundMgr.ConsolidateClips(microphoneAudioSource.clip, numSamples);
                microphoneAudioSource.clip = Microphone.Start(null, true, secondsToRecord, 16000);
                soundMgr.ClipStart();
            }
            else
            {
                DisplayMessage("Recording");
            }
        }

        if (bEndSomething)
        {
            int numSamples = Microphone.GetPosition(null);
            Microphone.End(null);
            soundMgr.ConsolidateClips(microphoneAudioSource.clip, numSamples);
            DisplayMessage("Stopping recording");

            // send the clip to be transacted
            soundMgr.Start();
        }
        if (Assets.Scripts.AITransactionHandler.HasNewModel)
        {
            DestroyOrInstantiateModel(true);
            if (model == null)
                model = Assets.Scripts.AITransactionHandler.GetFullModelFromCloud();
            DestroyOrInstantiateModel(false);
        }

        // rotate the cube.
        var theCube = GameObject.Find("Cube");
        theCube.transform.Rotate(Vector3.up, 10f * Time.deltaTime);

        // allow the player to move forward.
#if UNITY_HAS_GOOGLEVR && (UNITY_ANDROID || UNITY_EDITOR)
        if (GvrController.ClickButton)
        {
            // get the player and the camera to use for moving the player forward.
            var camera = GameObject.Find("Main Camera");

            // let's translate the camera forward
            player.transform.position += (camera.transform.rotation * Vector3.forward * Time.deltaTime) * m_forwardSpeed; // using deltatime means I'm moving at 1 meter/s

            // and keep the world canvas in scene.
            var mainTextCanvas = GameObject.Find("mainTextCanvas");
            var controllerPointer = GameObject.Find("GvrControllerPointer");

            mainTextCanvas.transform.position = controllerPointer.transform.position;
            mainTextCanvas.transform.position += (GvrController.Orientation * Vector3.forward) * 6;
            mainTextCanvas.transform.rotation = GvrController.Orientation;
        }

        // handle acceleration of the player.
        if (GvrController.IsTouching)
        {
            if (GvrController.TouchPos.y < .5 - deadZone)
            {
                // Should be accelerating
                m_forwardSpeed += 0.2f;
            }
            else if (GvrController.TouchPos.y > .5 + deadZone)
            {
                //Should be deaccelerating
                m_forwardSpeed -= 0.2f;
            }
        }

        // reset the speed when done moving.
        if (GvrController.TouchUp)
        {
            m_forwardSpeed = 3.0f;
        }
#endif
    }
}
