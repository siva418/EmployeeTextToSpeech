using Microsoft.AspNetCore.Mvc;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NamePronunciationTool.ServiceLayer;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Threading;
using System.Threading.Tasks;

namespace NamePronunciationTool.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SpeechController : ControllerBase
    {
        public VoiceList _voiceList { get; set; }
        public Authentication _authentication { get; set; }
        public IDBOperations _dBOperations { get; set; }
        public IConfiguration _configuration;
        public static string recoPhonemes;
        public static string _adEntIdToSaveSpeech;
        public ILogger<SpeechController> _ilogger;

        public string SubscriptionKey
        {
            get
            {
                return _configuration.GetValue<string>("subscriptionKey");
            }
        }

        private string yourServiceRegion = "eastus";
        private static bool completed = false;

        public SpeechController(ILogger<SpeechController> logger,VoiceList voiceList, Authentication authentication, IConfiguration configuration, IDBOperations dBOperations)
        {
            _authentication = authentication;
            _voiceList = voiceList;
            _configuration = configuration;
            _dBOperations = dBOperations;
            _ilogger = logger;
        }

        [HttpGet]
        public JsonResult Index()
        {
            return new JsonResult("HI! Welcome to Text to Speech API");
        }

        [HttpGet("TextToSpeech/{employeeAdEntId}/{name}/{region}")]
        public async Task<JsonResult> TextToSpeech(string employeeAdEntId, string name, string region)
        {
            try
            {
                var speechConfig = SpeechConfig.FromSubscription(SubscriptionKey, yourServiceRegion);

                speechConfig.SpeechSynthesisVoiceName = _voiceList.GetVoiceList(true).FirstOrDefault(x => x.Key == region).Value;


                using (var speechSynthesizer = new Microsoft.CognitiveServices.Speech.SpeechSynthesizer(speechConfig))
                {
                    var speechSynthesisResult = await speechSynthesizer.SpeakTextAsync(name ?? "Hi! Welcome to name pronunciation tool");
                    OutputSpeechSynthesisResult(speechSynthesisResult, name);
                }
                var phoneticName = GetPhoneticName(name);
                var isSaved = _dBOperations.SavePhoneticName(employeeAdEntId, phoneticName);
                if (!isSaved)
                {
                    return new JsonResult("Failure");
                }

                return new JsonResult("Success");
            }
            catch (Exception ex)
            {
                _ilogger.LogError(ex.Message);
                return new JsonResult(ex.Message);
            }
        }

        [HttpGet("GetPhoneticName/{name}")]
        public string GetPhoneticName(string name)
        {
            //txt to wav
            using (MemoryStream audioStream = new MemoryStream())
            {
                using (System.Speech.Synthesis.SpeechSynthesizer synth = new System.Speech.Synthesis.SpeechSynthesizer())
                {
                    synth.SetOutputToWaveStream(audioStream);
                    PromptBuilder pb = new PromptBuilder();
                    synth.Speak(name);
                    synth.SetOutputToNull();
                    audioStream.Position = 0;

                    recoPhonemes = string.Empty;
                    GrammarBuilder gb = new GrammarBuilder(name);
                    System.Speech.Recognition.Grammar g = new System.Speech.Recognition.Grammar(gb);
                    SpeechRecognitionEngine reco = new SpeechRecognitionEngine();
                    reco.SpeechHypothesized += new EventHandler<SpeechHypothesizedEventArgs>(Reco_SpeechHypothesized);
                    reco.SpeechRecognitionRejected += new EventHandler<SpeechRecognitionRejectedEventArgs>(Reco_SpeechRecognitionRejected);
                    reco.UnloadAllGrammars();
                    reco.LoadGrammar(g);
                    reco.SetInputToWaveStream(audioStream);
                    System.Speech.Recognition.RecognitionResult rr = reco.Recognize();
                    reco.SetInputToNull();
                    if (rr != null)
                    {
                        recoPhonemes = StringFromWordArray(rr.Words, WordType.Pronunciation);
                    }

                    return recoPhonemes;
                }
            }
        }

        private void Reco_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            byte[] dataToBeSaved;
            using (FileStream fileStream = new FileStream("test.wav", FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                e.Result.Audio.WriteToWaveStream(fileStream);
                using (var memoryStream = new MemoryStream())
                {
                    fileStream.CopyTo(memoryStream);
                    dataToBeSaved = memoryStream.ToArray();
                }
            }
            _dBOperations.SaveSpeech(_adEntIdToSaveSpeech, dataToBeSaved);

        }

        [HttpPost("SaveSpeech/{adentid}")]
        public JsonResult SaveSpeech(string adentid)
        {
            _adEntIdToSaveSpeech = adentid;
            // Create an in-process speech recognizer for the en-US locale.  
            using (
            SpeechRecognitionEngine recognizer =
              new SpeechRecognitionEngine(
                new System.Globalization.CultureInfo("en-US")))
            {

                // Create and load a dictation grammar.  
                recognizer.LoadGrammar(new DictationGrammar());

                // Add a handler for the speech recognized event.  
                recognizer.SpeechRecognized +=
                  new EventHandler<SpeechRecognizedEventArgs>(Reco_SpeechRecognized);

                recognizer.RecognizeCompleted +=
                  new EventHandler<RecognizeCompletedEventArgs>(RecognizeCompletedHandler);

                // Configure input to the speech recognizer.  
                recognizer.SetInputToDefaultAudioDevice();
                completed = false;
                // Start asynchronous, continuous speech recognition.  
                recognizer.RecognizeAsync(RecognizeMode.Single);

                while (!completed)
                {
                    Thread.Sleep(1000);
                }

            }


            return new JsonResult("Success");
        }

        [HttpGet("GetVoices/{region}")]
        public async Task<string> GetVoiceList(string region)
        {
            HttpClient httpClient = new HttpClient();
            var fetchUri = "https://eastus.tts.speech.microsoft.com/cognitiveservices/voices/list";
            httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", SubscriptionKey);
            var token = await _authentication.FetchTokenAsync("https://eastus.api.cognitive.microsoft.com/sts/v1.0/issueToken", SubscriptionKey);
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
            UriBuilder uriBuilder = new UriBuilder(fetchUri);

            var result = await httpClient.GetAsync(uriBuilder.Uri.AbsoluteUri);
            return await result.Content.ReadAsStringAsync();
        }

        public void OutputSpeechSynthesisResult(SpeechSynthesisResult speechSynthesisResult, string text)
        {
            switch (speechSynthesisResult.Reason)
            {
                case ResultReason.SynthesizingAudioCompleted:
                    Console.WriteLine($"Speech synthesized for text: [{text}]");
                    break;
                case ResultReason.Canceled:
                    var cancellation = SpeechSynthesisCancellationDetails.FromResult(speechSynthesisResult);
                    Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                    if (cancellation.Reason == CancellationReason.Error)
                    {
                        Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                        Console.WriteLine($"CANCELED: ErrorDetails=[{cancellation.ErrorDetails}]");
                        Console.WriteLine($"CANCELED: Did you set the speech resource key and region values?");
                    }
                    break;
                default:
                    break;
            }
        }

        public static string StringFromWordArray(ReadOnlyCollection<RecognizedWordUnit> words, WordType type)
        {
            string text = "";
            foreach (RecognizedWordUnit word in words)
            {
                string wordText = "";
                if (type == WordType.Text || type == WordType.Normalized)
                {
                    wordText = word.Text;
                }
                else if (type == WordType.Lexical)
                {
                    wordText = word.LexicalForm;
                }
                else if (type == WordType.Pronunciation)
                {
                    wordText = word.Pronunciation;
                    //MessageBox.Show(word.LexicalForm);
                }
                else
                {
                    throw new InvalidEnumArgumentException(String.Format("[0}: is not a valid input", type));
                }
                //Use display attribute

                if ((word.DisplayAttributes & DisplayAttributes.OneTrailingSpace) != 0)
                {
                    wordText += " ";
                }
                if ((word.DisplayAttributes & DisplayAttributes.TwoTrailingSpaces) != 0)
                {
                    wordText += "  ";
                }
                if ((word.DisplayAttributes & DisplayAttributes.ConsumeLeadingSpaces) != 0)
                {
                    wordText = wordText.TrimStart();
                }
                if ((word.DisplayAttributes & DisplayAttributes.ZeroTrailingSpaces) != 0)
                {
                    wordText = wordText.TrimEnd();
                }

                text += wordText;

            }
            return text;
        }

        public static void Reco_SpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {
            recoPhonemes = StringFromWordArray(e.Result.Words, WordType.Pronunciation);
        }

        public static void Reco_SpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            recoPhonemes = StringFromWordArray(e.Result.Words, WordType.Pronunciation);
        }

        public static void RecognizeCompletedHandler(object sender, RecognizeCompletedEventArgs e)
        {
            Console.WriteLine(" In RecognizeCompletedHandler.");

            if (e.Error != null)
            {
                Console.WriteLine(
                  " - Error occurred during recognition: {0}", e.Error);
                return;
            }
            if (e.InitialSilenceTimeout || e.BabbleTimeout)
            {
                Console.WriteLine(
                  " - BabbleTimeout = {0}; InitialSilenceTimeout = {1}",
                  e.BabbleTimeout, e.InitialSilenceTimeout);
                return;
            }
            if (e.InputStreamEnded)
            {
                Console.WriteLine(
                  " - AudioPosition = {0}; InputStreamEnded = {1}",
                  e.AudioPosition, e.InputStreamEnded);
            }
            if (e.Result != null)
            {
                Console.WriteLine(
                  " - Grammar = {0}; Text = {1}; Confidence = {2}",
                  e.Result.Grammar.Name, e.Result.Text, e.Result.Confidence);
                Console.WriteLine(" - AudioPosition = {0}", e.AudioPosition);
            }
            else
            {
                Console.WriteLine(" - No result.");
            }

            completed = true;
        }

    }
}
