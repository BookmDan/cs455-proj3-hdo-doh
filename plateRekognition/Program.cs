using System;
using System.IO;
using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Amazon.S3;
using Amazon.S3.Model;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Textract;
using Amazon.Textract.Model;

namespace DriversCamera;

public class plateRekognition
{

    static async Task Main(string[] args) {

        try {
            await StartDetectSampleAsync();
            await DetectSampleAsync();
        }
        catch (Exception e) {
            Console.WriteLine(e.Message);
        }
    }
    private static async Task DetectSampleAsync()
    {
        using (var textractClient = new AmazonTextractClient(RegionEndpoint.USEast1))
        {
            string path = @"C:\Users\admin\Downloads\Module25.zip\Module25\Documents\LicensePlate1.jpg";
            var bytes = File.ReadAllBytes(path);

            Console.WriteLine("Detect Document Text");
            var detectResponse = await textractClient.DetectDocumentTextAsync(new DetectDocumentTextRequest
            {
                Document = new Document { Bytes = new MemoryStream(bytes) }
            });

            foreach (var block in detectResponse.Blocks)
            {
                Console.WriteLine($"Block Type: {block.BlockType}");
                Console.WriteLine($"Confidence: {block.Confidence}");
                Console.WriteLine($"Text: {block.Text}");
            }
        }
    }
    private static async Task StartDetectSampleAsync()
    {
        var s3Bucket = "drivers-camera-bucket";
        var fileName = "LicensePlate1.jpg";

        using (var textractClient = new AmazonTextractClient(RegionEndpoint.USEast1)) ;
        using (var s3Client = new AmazonS3Client(RegionEndpoint.USEast1)) ;
        {
            Console.WriteLine($"Uplaod {fileName} to {s3Bucket} bucket");
            var putRequest = new PutObjectRequest
            {
                BucketName = s3Bucket,
                FilePath = fileName,
                Key = Path.GetFileName(fileName)
            
                //InputStream = File.OpenRead(@"C:\Users\admin\Downloads\Module25.zip\Module25\Documents\LicensePlate1.jpg")
            };

            await s3Client.PutObjectAsync(putRequest);

            Console.WriteLine("Start Detect Document Text");
            var startResponse await textractClient.StartDocumentTextDetectionAsync(new StartDocumentTextDetectionRequest
            {
                DocumentLocation = new DocumentLocation
                {
                    S3Object = new Amazon.Textract.Model.S3Object
                    {
                        Bucket = s3Bucket,
                        Name = putRequest.Key
                    }
                }
            });

            Console.WriteLine($"Job Id: {startResponse.JobId}");
            var getDetectionRequest = new GetDocumentTextDetectionRequest
            {
                JobId = startResponse.JobId
            };

            Console.WriteLine("Poll for detct text job to complete");
            GetDocumentTextDetectionResponse getDetectionResponse = null;
            do
            {
                Thread.Sleep(1000);
                getDetectionResponse = await textractClient.GetDocumentTextDetectionAsync(getDetectionRequest);
            } while (getDetectionResponse.JobStatus == JobStatus.SUCCEEDED){
                do
                {
                    foreach (var block in getDetectionResponse.Blocks)
                    {
                        Console.WriteLine($"Block Type: {block.BlockType}");
                        Console.WriteLine($"Confidence: {block.Confidence}");
                        Console.WriteLine($"Text: {block.Text}");
                    }
                    if (string.IsNullOrEmpty(getDetectionResponse.NextToken))
                    {
                        break;
                    }

                    getDetectionRequest.NextToken = getDetectionResponse.NextToken;
                    getDetectionResponse = await textractClient.GetDocumentTextDetectionAsync(getDetectionRequest);
                } while (!string.IsNullOrEmpty(getDetectionResponse.NextToken));
            } else
            {
                Console.WriteLine($"Job Failed: {getDetectionResponse.StatusMessage}");
            }
        }

    }


        byte[] docBytes = FileToByteArray(path);
            Document doc = new Document();
            doc.Bytes = new MemoryStream(docBytes);

            DetectDocumentTextRequest req = new DetectDocumentTextRequest()
            {
                Document = doc
            };

        }
    }


    string path = @"C:\Users\admin\Downloads\Module25.zip\Module25\Documents\LicensePlate1.jpg";
    byte[] docBytes = FileToByteArray(path);
        Document doc = new Document();
        doc.Bytes = new MemoryStream(docBytes);

        DetectDocumentTextRequest req = new DetectDocumentTextRequest()
        {
            Document = doc
        };

        try
        {
            SharedCredentialsFile sharedCredFile = new SharedCredentialsFile();
            CredentialProfile defaultProfile = GetDefaultProfile(sharedCredFile);

            if (defaultProfile != null)
            {
                AWSCredentials credentials = AWSCredentialsFactory.GetAWSCredentials(defaultProfile, new SharedCredentialsFile());
                AmazonTextractClient client = new AmazonTextractClient(credentials, Amazon.RegionEndpoint.USEast1);

                Task<DetectDocumentTextResponse> res = client.DetectDocumentTextAsync(req, new System.Threading.CancellationToken());
                while (true)
                {
                    if (res.Status == TaskStatus.RanToCompletion)
                    {
                        foreach (Block b in res.Result.Blocks)
                        {
                            Console.WriteLine(b.Text);
                        }
                        break;
                    }
                    Thread.Sleep(1000);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: {0}", ex.Message);
        }
        Console.ReadLine();

    }

    
    private static Task StartDetectSampleAsync()
    {
        throw new NotImplementedException();
    }

    private static byte[] FileToByteArray(string path)
    {
        byte[] byteArray = null;
        using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
        {
            using (BinaryReader reader = new BinaryReader(fs))
            {
                byteArray = reader.ReadBytes((int)(new FileInfo(path).Length));
            }
        }
        return byteArray;
    }
`
    private static CredentialProfile GetDefaultProfile(SharedCredentialsFile sharedCredFile)
    {
        throw new NotImplementedException();
    }
}


