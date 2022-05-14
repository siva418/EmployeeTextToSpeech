using Microsoft.AspNetCore.Mvc;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
        public async Task<JsonResult> Index()
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

                using (var speechSynthesizer = new SpeechSynthesizer(speechConfig))
                {
                    var speechSynthesisResult = await speechSynthesizer.SpeakTextAsync(name ?? "Hi! Welcome to GPS");
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
    }
}
