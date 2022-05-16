using Microsoft.AspNetCore.Mvc;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NamePronunciation.Models;
using NamePronunciationTool.ServiceLayer;
using NAudio.Wave;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

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

        [HttpPost("TextToSpeech")]
        public async Task<JsonResult> TextToSpeech([FromBody]SpeechModel speechModel)
        {
            string accessToken = await _authentication.FetchTokenAsync("https://eastus.api.cognitive.microsoft.com/sts/v1.0/issueToken", SubscriptionKey);
            string host = "https://eastus.tts.speech.microsoft.com/cognitiveservices/v1";

            // Create SSML document.
            XDocument body = new XDocument(
                    new XElement("speak",
                        new XAttribute("version", "1.0"),
                        new XAttribute(XNamespace.Xml + "lang", "en-US"),
                        new XElement("voice",
                            new XAttribute(XNamespace.Xml + "lang", "en-US"),
                            new XAttribute(XNamespace.Xml + "gender", "Male"),
                            new XAttribute("name", "en-IN-PrabhatNeural"), // Short name for "Microsoft Server Speech Text to Speech Voice (en-US, Jessa24KRUS)"
                            speechModel.Name)));

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    using (HttpRequestMessage request = new HttpRequestMessage())
                    {
                        // Set the HTTP method
                        request.Method = HttpMethod.Post;
                        // Construct the URI
                        request.RequestUri = new Uri(host);
                        // Set the content type header
                        request.Content = new StringContent(body.ToString(), Encoding.UTF8, "application/ssml+xml");
                        // Set additional header, such as Authorization and User-Agent
                        request.Headers.Add("Authorization", "Bearer " + accessToken);
                        request.Headers.Add("Connection", "Keep-Alive");
                        // Update your resource name
                        request.Headers.Add("User-Agent", "TexttoSpeech");
                        // Audio output format. See API reference for full list.
                        request.Headers.Add("X-Microsoft-OutputFormat", "riff-24khz-16bit-mono-pcm");
                        // Create a request
                        using (HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false))
                        {
                            response.EnsureSuccessStatusCode();
                            // Asynchronously read the response
                            using (Stream dataStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                            {
                                using (FileStream fileStream = new FileStream(speechModel.EmployeeAdEntId+ ".mp3", FileMode.Create, FileAccess.Write, FileShare.Write))
                                {
                                    await dataStream.CopyToAsync(fileStream).ConfigureAwait(false);
                                    fileStream.Close();

                                    var result = new HttpResponseMessage();
                                    result.StatusCode = response.StatusCode;
                                    if (response.StatusCode == HttpStatusCode.OK)
                                        result.Content = new StringContent("Audio Saved");
                                    else
                                        result.Content = new StringContent(response.ReasonPhrase);

                                    using (var audioFile = new AudioFileReader(speechModel.EmployeeAdEntId + ".mp3"))
                                    {
                                        using (var outputDevice = new WaveOutEvent())
                                        {
                                            outputDevice.Init(audioFile);
                                            outputDevice.Play();
                                            while (outputDevice.PlaybackState == PlaybackState.Playing)
                                            {
                                                Thread.Sleep(1000);
                                            }
                                        }
                                    }

                                    return new JsonResult(result);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var result = new HttpResponseMessage();
                result.Content = new StringContent(ex.Message + ex.StackTrace);
                result.StatusCode = HttpStatusCode.InternalServerError;
                return new JsonResult(result);
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
            using (FileStream fileStream = new FileStream(_adEntIdToSaveSpeech+".mp3", FileMode.OpenOrCreate, FileAccess.ReadWrite))
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

        [HttpPost("SaveSpeech")]
        public JsonResult SaveSpeech([FromBody] SpeechModel speechModel)
        {
            _adEntIdToSaveSpeech = speechModel.EmployeeAdEntId;
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

        [HttpGet("PlayAudio/{adentid}")]
        public void PlayAudio(string adentid)
        {
            using (var audioFile = new AudioFileReader(adentid + ".mp3"))
            {
                using (var outputDevice = new WaveOutEvent())
                {
                    outputDevice.Init(audioFile);
                    outputDevice.Play();
                    while (outputDevice.PlaybackState == PlaybackState.Playing)
                    {
                        Thread.Sleep(1000);
                    }
                }
            }
        }

        [HttpGet("SaveType/{type}/{adentid}")]
        public void SaveType(string type,string adentid)
        {
            _dBOperations.SaveSpeechType(type,adentid);
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
