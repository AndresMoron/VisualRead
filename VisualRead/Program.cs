
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


namespace analisis_imagenes
{

    public class Program
    {
        // Variables de conexion del portal de azure
        static string subscriptionKey = "1d0ea4261e724467b0730e19e9e57276";
        static string endpoint = "https://visionreadandres.cognitiveservices.azure.com/";

       // private static List<string> imagenes = new List<string> { "upsa.jpg", "salar.jpg", "microsoft.jpg", "celebrities.jpg" };
        public static List<string> textos = new List<string> { "texto.jpg","texto2.jpg"};


        static void Main(string[] args)
        {
            Console.WriteLine("Azure Cognitive Services - Computer Vision");
            Console.WriteLine();

            ComputerVisionClient client = Authenticate(endpoint, subscriptionKey); //Instanciamos el cliente 

            //Local porque se almacena localmente en la PC, tambien hay la opcion de pasar URL a imagenes en internet.

            foreach (var texto in textos)
            {
                // Extraer texto de una imagen local
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

        //Analisis de imagenes
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

                // Celebrities in image, if any.
                if (null != results.Categories)
                {
                    Console.WriteLine("Celebrities:");
                    foreach (var category in results.Categories)
                    {
                        if (category.Detail?.Celebrities != null)
                        {
                            foreach (var celeb in category.Detail.Celebrities)
                            {
                                Console.WriteLine($"{celeb.Name} with confidence {celeb.Confidence} at location {celeb.FaceRectangle.Left}, " +
                                  $"{celeb.FaceRectangle.Top},{celeb.FaceRectangle.Height},{celeb.FaceRectangle.Width}");
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
        //Fin de analisis de imagenes

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
        //Fin de dominio especifico

        //Extraer texto
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
/*
namespace VisualRead
{
    class Program
    {
        // Add your Computer Vision subscription key and endpoint
        static string subscriptionKey = "1d0ea4261e724467b0730e19e9e57276";
        static string endpoint = "https://visionreadandres.cognitiveservices.azure.com/";
        // URL image used for analyzing an image (image of puppy)
        private const string ANALYZE_URL_IMAGE = "https://azurecomcdn.azureedge.net/cvt-751c3316019c3a45b91ae729e81ed0fce378619280a8bf91489fe735368537a4/images/shared/cognitive-services-demos/analyze-image/analyze-5-thumbnail.jpg";
        
        static void Main(string[] args)
        {
            // Create a client
            ComputerVisionClient client = Authenticate(endpoint, subscriptionKey);

            // Analyze an image to get features and other properties.
            AnalyzeImageUrl(client, ANALYZE_URL_IMAGE).Wait();

            Console.WriteLine("----------------------------------------------------------");
            Console.WriteLine();
            Console.WriteLine("Computer Vision quickstart is complete.");
            Console.WriteLine();
            Console.WriteLine("Press enter to exit...");
            Console.ReadLine();

        }
        public static ComputerVisionClient Authenticate(string endpoint, string key)
        {

            ComputerVisionClient client =
              new ComputerVisionClient(new ApiKeyServiceClientCredentials(key))
              { Endpoint = endpoint };
            return client;
        }

            public static async Task AnalyzeImageUrl(ComputerVisionClient client, string imageUrl)
            {
                Console.WriteLine("----------------------------------------------------------");
                Console.WriteLine("ANALYZE IMAGE - URL");
                Console.WriteLine();

                // Creating a list that defines the features to be extracted from the image. 
                List<VisualFeatureTypes?> features = new List<VisualFeatureTypes?>()
            {
            VisualFeatureTypes.Categories, VisualFeatureTypes.Description,
            //VisualFeatureTypes.Tags, VisualFeatureTypes.Adult,
            VisualFeatureTypes.Objects

            };
                Console.WriteLine($"Analyzing the image {Path.GetFileName(imageUrl)}...");
                Console.WriteLine();

                // Analyze the URL image 
                ImageAnalysis results = await client.AnalyzeImageAsync(imageUrl, visualFeatures: features);

                // Sunmarizes the image content.
                Console.WriteLine("Summary:");
                foreach (var caption in results.Description.Captions)
                {
                    Console.WriteLine($"{caption.Text} with confidence {caption.Confidence}");
                }
                Console.WriteLine();
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

        // Display categories the image is divided into.
        /* Console.WriteLine("Categories:");

         foreach (var category in results.Categories)
         {
             Console.WriteLine($"{category.Name} with confidence {category.Score}");
         }
         Console.WriteLine();
     // Image tags and their confidence score
     Console.WriteLine("Tags:");
     foreach (var tag in results.Tags)
     {
         Console.WriteLine($"{tag.Name} {tag.Confidence}");
     }
     Console.WriteLine();
     // </snippet_tags>

     // <snippet_objects>
     // Objects
     Console.WriteLine("Objects:");
     foreach (var obj in results.Objects)
     {
         Console.WriteLine($"{obj.ObjectProperty} with confidence {obj.Confidence} at location {obj.Rectangle.X}, " +
           $"{obj.Rectangle.X + obj.Rectangle.W}, {obj.Rectangle.Y}, {obj.Rectangle.Y + obj.Rectangle.H}");
     }
     Console.WriteLine();

     Console.WriteLine("Brands:");
     foreach (var brand in results.Brands)
     {
         Console.WriteLine($"Logo of {brand.Name} with confidence {brand.Confidence} at location {brand.Rectangle.X}, " +
           $"{brand.Rectangle.X + brand.Rectangle.W}, {brand.Rectangle.Y}, {brand.Rectangle.Y + brand.Rectangle.H}");
     }
     Console.WriteLine();

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

    }

}*/