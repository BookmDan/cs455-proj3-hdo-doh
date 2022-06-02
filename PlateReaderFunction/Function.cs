using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using TicketProcessingFunction;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace PlateReaderFunction;

public class Function
{
    IAmazonS3 S3Client { get; set; }    
    /// <summary>
    /// A simple function that takes a string and does a ToUpper
    /// </summary>
    /// <param name="input"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    /// 
    // default Constructor invoked in a Lambda environment. 
    // AWS credentials will come from IAM role associated with the function and the AWS region will be set to the
    /// region the Lambda function is executed in.
    ///  
    public Function()
    {
        S3Client = new AmazonS3Client();
    }

    public Function(IAmazonS3 s3Client)
    {
        this.S3Client = s3Client;
    }

    public async Task<string> FunctionHandler(S3Event evnt, ILambdaContext context)
    {
        var s3Event = evnt.Records?[0].S3;
        if (s3Event == null)
        {
            return null;
        }
        try
        {
            string fileName = s3Event.Object.Key;
            GetObjectResponse response = await S3Client.GetObjectAsync(s3Event.Bucket.Name, s3Event.Object.Key);
            Stream stream = await S3Client.GetObjectStreamAsync(s3Event.Bucket.Name, s3Event.Object.Key, null);
            Console.WriteLine("File Name: " + s3Event.Object.Key);
            string fileContent;
            using (StreamReader reader = new StreamReader(stream))
            {
                fileContent = reader.ReadToEnd();
                reader.Close();
            }

            // should read the digital image and send it to the plate reader
            return String.Empty;
        }
        catch (Exception e)
        {
            context.Logger.LogInformation($"Error getting object {s3Event.Object.Key} from bucket {s3Event.Bucket.Name}. Make sure they exist and your bucket is in the same region as this function.");
            context.Logger.LogInformation(e.Message);
            context.Logger.LogInformation(e.StackTrace);
            throw;
        }
    }

    //sends licenseplate to DownwardQueue
    public static async Task SendIDToDownwardQueueAsync(string licensePlate)
    {
        try
        {
            AmazonSQSClient sqsClient = new AmazonSQSClient();
            SendMessageRequest request = new SendMessageRequest
            {
                QueueUrl = @"https://sqs.us-east-1.amazonaws.com/166874187343/driversDownQueue",
                MessageBody = licensePlate
            };

            SendMessageResponse response = await sqsClient.SendMessageAsync(request);
        }
        catch (AmazonSQSException e)
        {
            Console.WriteLine(
                    "Error encountered ***. Message:'{0}' when writing an object"
                    , e.Message);
        }
        catch (Exception e)
        {
            Console.WriteLine(
                "Unknown encountered on server. Message:'{0}' when writing an object"
                , e.Message);
        }
    }

    private static string ProcessLicense (string fileContent)
    {
        DriverUpQueueMessage tab = new DriverUpQueueMessage();
        // get rekognition 
        tab.isFromCali = true;
    }

}

