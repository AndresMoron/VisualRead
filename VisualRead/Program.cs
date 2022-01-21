using System;
using System.Collections.Generic;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Linq;


namespace VisualRead
{

    public class Program
    {
       
        static string subscriptionKey = "1d0ea4261e724467b0730e19e9e57276";
        static string endpoint = "https://visionreadandres.cognitiveservices.azure.com/";

        public static List<string> textos = new List<string> { "placa.jpg", "auto.jpg" };

        static void Main(string[] args)
        {
            Console.WriteLine("Azure Cognitive Services - Computer Vision");
            Console.WriteLine();

            ComputerVisionClient client = Authenticate(endpoint, subscriptionKey);  


            foreach (var texto in textos)
            {
                // Local Image
                ReadFileLocal(client, texto).Wait();
            }

            Console.WriteLine("Programa ejecutado exitosamente");
        }

        //Metodo de autentificacion
        public static ComputerVisionClient Authenticate(string endpoint, string key)
        {
            ComputerVisionClient client =
              new ComputerVisionClient(new ApiKeyServiceClientCredentials(key))
              { Endpoint = endpoint };
            return client;
        }

        //Image Analysis
        public static async Task AnalyzeImageLocal(ComputerVisionClient client, string localImage)
        {
            Console.WriteLine("----------------------------------------------------------");
            Console.WriteLine("ANALYZE IMAGE - LOCAL IMAGE");
            Console.WriteLine();

            // Creating a list that defines the features to be extracted from the image. 
            List<VisualFeatureTypes?> features = new List<VisualFeatureTypes?>()
            {
                VisualFeatureTypes.Categories, VisualFeatureTypes.Description,
                VisualFeatureTypes.Faces, VisualFeatureTypes.ImageType,
                VisualFeatureTypes.Tags, VisualFeatureTypes.Adult,
                VisualFeatureTypes.Color, VisualFeatureTypes.Brands,
                VisualFeatureTypes.Objects
            };

            Console.WriteLine($"Analyzing the local image {Path.GetFileName(localImage)}...");
            Console.WriteLine();

            using (Stream analyzeImageStream = File.OpenRead(localImage))
            {
                // Analyze the local image.
                ImageAnalysis results = await client.AnalyzeImageInStreamAsync(analyzeImageStream, visualFeatures: features);

                // Sunmarizes the image content.
                if (null != results.Description && null != results.Description.Captions)
                {
                    Console.WriteLine("Summary:");
                    foreach (var caption in results.Description.Captions)
                    {
                        Console.WriteLine($"{caption.Text} with confidence {caption.Confidence}");
                    }
                    Console.WriteLine();
                }

                // Display categories the image is divided into.
                Console.WriteLine("Categories:");
                foreach (var category in results.Categories)
                {
                    Console.WriteLine($"{category.Name} with confidence {category.Score}");
                }
                Console.WriteLine();

                // Image tags and their confidence score
                if (null != results.Tags)
                {
                    Console.WriteLine("Tags:");
                    foreach (var tag in results.Tags)
                    {
                        Console.WriteLine($"{tag.Name} {tag.Confidence}");
                    }
                    Console.WriteLine();
                }

                // Objects
                if (null != results.Objects)
                {
                    Console.WriteLine("Objects:");
                    foreach (var obj in results.Objects)
                    {
                        Console.WriteLine($"{obj.ObjectProperty} with confidence {obj.Confidence} at location {obj.Rectangle.X}, " +
                          $"{obj.Rectangle.X + obj.Rectangle.W}, {obj.Rectangle.Y}, {obj.Rectangle.Y + obj.Rectangle.H}");
                    }
                    Console.WriteLine();
                }


                // Well-known brands, if any.
                if (null != results.Brands)
                {
                    Console.WriteLine("Brands:");
                    foreach (var brand in results.Brands)
                    {
                        Console.WriteLine($"Logo of {brand.Name} with confidence {brand.Confidence} at location {brand.Rectangle.X}, " +
                          $"{brand.Rectangle.X + brand.Rectangle.W}, {brand.Rectangle.Y}, {brand.Rectangle.Y + brand.Rectangle.H}");
                    }
                    Console.WriteLine();
                }

                // Popular landmarks in image, if any.
                if (null != results.Categories)
                {
                    Console.WriteLine("Landmarks:");
                    foreach (var category in results.Categories)
                    {
                        if (category.Detail?.Landmarks != null)
                        {
                            foreach (var landmark in category.Detail.Landmarks)
                            {
                                Console.WriteLine($"{landmark.Name} with confidence {landmark.Confidence}");
                            }
                        }
                    }
                    Console.WriteLine();
                }

                // Identifies the color scheme.
                if (null != results.Color)
                {
                    Console.WriteLine("Color Scheme:");
                    Console.WriteLine("Is black and white?: " + results.Color.IsBWImg);
                    Console.WriteLine("Accent color: " + results.Color.AccentColor);
                    Console.WriteLine("Dominant background color: " + results.Color.DominantColorBackground);
                    Console.WriteLine("Dominant foreground color: " + results.Color.DominantColorForeground);
                    Console.WriteLine("Dominant colors: " + string.Join(",", results.Color.DominantColors));
                    Console.WriteLine();
                }

                // Detects the image types.
                if (null != results.ImageType)
                {
                    Console.WriteLine("Image Type:");
                    Console.WriteLine("Clip Art Type: " + results.ImageType.ClipArtType);
                    Console.WriteLine("Line Drawing Type: " + results.ImageType.LineDrawingType);
                    Console.WriteLine();
                }
            }
        }
        

        //Dominio especifico
        public static async Task DetectDomainSpecific(ComputerVisionClient client, string localImage)
        {
            Console.WriteLine("----------------------------------------------------------");
            Console.WriteLine("DETECT DOMAIN-SPECIFIC CONTENT -  LOCAL IMAGE");
            Console.WriteLine();

            // Detect the domain-specific content in a local image.
            using (Stream imageStream = File.OpenRead(localImage))
            {
                // Change "celebrities" to "landmarks" if that is the domain you are interested in.
                DomainModelResults resultsLocal = await client.AnalyzeImageByDomainInStreamAsync("landmarks", imageStream);
                Console.WriteLine($"Detecting landmarks in the local image {Path.GetFileName(localImage)}...");
                // Display results.
                var jsonLocal = JsonConvert.SerializeObject(resultsLocal.Result);
                JObject resultJsonLocal = JObject.Parse(jsonLocal);
                if (resultJsonLocal["landmarks"].Any())
                {
                    Console.WriteLine($"Landmarks detected: {resultJsonLocal["landmarks"][0]["name"]} " +
                      $"with confidence {resultJsonLocal["landmarks"][0]["confidence"]}");
                }
            }
            Console.WriteLine();
        }
        

        //Text
        public static async Task ReadFileLocal(ComputerVisionClient client, string localFile)
        {
            Console.WriteLine("----------------------------------------------------------");
            Console.WriteLine("READ FILE FROM LOCAL");
            Console.WriteLine();

            // Read text from URL
            var textHeaders = await client.ReadInStreamAsync(File.OpenRead(localFile));
            // After the request, get the operation location (operation ID)
            string operationLocation = textHeaders.OperationLocation;
            Thread.Sleep(2000);

            // <snippet_extract_response>
            // Retrieve the URI where the recognized text will be stored from the Operation-Location header.
            // We only need the ID and not the full URL
            const int numberOfCharsInOperationId = 36;
            string operationId = operationLocation.Substring(operationLocation.Length - numberOfCharsInOperationId);

            // Extract the text
            ReadOperationResult results;
            Console.WriteLine($"Reading text from local file {Path.GetFileName(localFile)}...");
            Console.WriteLine();
            do
            {
                results = await client.GetReadResultAsync(Guid.Parse(operationId));
            }
            while ((results.Status == OperationStatusCodes.Running ||
                results.Status == OperationStatusCodes.NotStarted));
            // </snippet_extract_response>

            // <snippet_extract_display>
            // Display the found text.
            Console.WriteLine();
            var textUrlFileResults = results.AnalyzeResult.ReadResults;
            foreach (ReadResult page in textUrlFileResults)
            {
                foreach (Line line in page.Lines)
                {
                    Console.WriteLine(line.Text);
                }
            }
            Console.WriteLine();
        }
        //Fin de extraer texto
    }
}


