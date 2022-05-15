using Microsoft.AspNetCore.Mvc;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Threading.Tasks;

namespace EmployeeTextToSpeech.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SpeechController : ControllerBase
    {
        public VoiceList _voiceList { get; set; }
        public Authentication _authentication { get; set; }
        public IConfiguration _configuration;

        public string SubscriptionKey
        {
            get
            {
                return _configuration.GetValue<string>("subscriptionKey");
            }
        }

        private string yourServiceRegion = "eastus";

        public SpeechController( VoiceList voiceList, Authentication authentication, IConfiguration configuration)
        {
            _authentication = authentication;
            _voiceList = voiceList;
            _configuration = configuration;
        }

        [HttpGet]
        public JsonResult Index()
        {
            return new JsonResult("HI! Welcome to Text to Speech API");
        }

        [HttpGet("TextToSpeech/{name}/{region}")]
        public async Task<JsonResult> TextToSpeech(string name, string region)
        {
            try
            {
                var speechConfig = SpeechConfig.FromSubscription(SubscriptionKey, yourServiceRegion);

                speechConfig.SpeechSynthesisVoiceName = _voiceList.GetVoiceList(true).FirstOrDefault(x => x.Key == region).Value;
                

                using (var speechSynthesizer = new Microsoft.CognitiveServices.Speech.SpeechSynthesizer(speechConfig))
                {
                    var speechSynthesisResult = await speechSynthesizer.SpeakTextAsync(name ?? "Hi! Welcome to GPS");
                    var a = speechSynthesisResult.Properties;
                    OutputSpeechSynthesisResult(speechSynthesisResult, name);
                }
                return new JsonResult("Success");
            }
            catch(Exception)
            {
                return new JsonResult("An error occured");
            }
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

        [HttpGet("GetVoices/{region}")]
        public async Task<string> GetVoiceList(string region)
        {
            HttpClient httpClient = new HttpClient();
            var fetchUri = "https://eastus.tts.speech.microsoft.com/cognitiveservices/voices/list";
            httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", SubscriptionKey);
            var token = await _authentication.FetchTokenAsync("https://eastus.api.cognitive.microsoft.com/sts/v1.0/issueToken", SubscriptionKey);
            httpClient.DefaultRequestHeaders.Add("Authorization","Bearer "+token);
            UriBuilder uriBuilder = new UriBuilder(fetchUri);

            var result = await httpClient.GetAsync(uriBuilder.Uri.AbsoluteUri);
            return await result.Content.ReadAsStringAsync();
        }

        [HttpGet("GetPhoneticName/{name}")]
        public JsonResult GetPhoneticName(string name)
        {
            //this is a trick to figure out phonemes used by synthesis engine

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

                    return new JsonResult(recoPhonemes);
                }
            }
        }
        public static string recoPhonemes;

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

    }
}
