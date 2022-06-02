using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Xml;
using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon;       //to specify region when creating client
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using System.Text.Json;

namespace DMVService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private const string downQueueURL = @"https://sqs.us-east-1.amazonaws.com/166874187343/driversDownQueue";
    private const string upQueueURL = @"https://sqs.us-east-1.amazonaws.com/166874187343/driversUpQueue";

    //private const string logPath = @"C:\Temp\InsuranceDataService.log"; //location of log file

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {

        await base.StartAsync(cancellationToken);

    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        //Get AWS credentials
        AWSCredentials credentials = GetAWSCredentialsByName("default");
        AmazonSQSClient sqsClient = new AmazonSQSClient(credentials, RegionEndpoint.USEast1);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // long-poll the DownwardQueue for incoming messages by waiting 20 seconds 
                // (the maximum wait time) to poll DownwardQueue for incoming messages
                var msg = await GetMessage(sqsClient, downQueueURL, 20);
                WriteToLog("******************************************");
                // if there are messages 
                if (msg.Messages.Count != 0)
                {

                    string licenseTab = msg.Messages[0].Body;
                    // write to log upon message receipt
                    WriteToLog("Read Message: \t" + licenseTab);

                    // query from xml database 
                    string queriedDMV = processXML(licenseTab);
                    WriteToLog("Just queried DMV Database");

                    //  deletes the message from the DownwardQueue
                    DeleteMessage(sqsClient, msg.Messages[0], downQueueURL).Wait();
                    WriteToLog("Just deleted message from down queue");

                    // put a response message to upward queue
                    SendMessageToQueueAsync(queriedDMV, sqsClient).Wait();
                    WriteToLog("Just sent XML To UpwardQueue");

                    // write to log the message posted
                    WriteToLog("Posted Message: \t" + queriedDMV);

                }
                else
                {
                    WriteToLog("No messages polled from queue.");
                }
                WriteToLog("******************************************");
                await Task.Delay(1000, stoppingToken);
            }
            catch (Exception ex)
            {
                WriteToLog(ex.Message);
            }

        }
    }

    //Sends a message to UpwardQueue
    static async Task SendMessageToQueueAsync(string message, AmazonSQSClient sqsClient)
    {
        try
        {
            SendMessageRequest request = new SendMessageRequest
            {
                QueueUrl = upQueueURL,
                MessageBody = message
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


    // Method to read a message from the given queue
    // In this example, it gets one message at a time
    private static async Task<ReceiveMessageResponse> GetMessage(
      IAmazonSQS sqsClient, string qUrl, int waitTime = 0)
    {
        return await sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = qUrl,
            MaxNumberOfMessages = 1,
            WaitTimeSeconds = waitTime
        });
    }

    // need to construct a json string with patientID, hasInsurance, policyNumber, provider
    private static string processXML(String licenseTab)
    {
        // Instantiate a PatientUQMessage object to hold patient's insurance information
        // before serializing it into a json string
        DriverUpQueueMessage info = new DriverUpQueueMessage();
        info.queriedDMV = queriedDMV;

        // Query patient information from the xml database using Xpath
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.Load(@"C:\Users\admin\AppData\Local\Temp\Temp1_Project3.zip\Project3\DMVDatabase.xml");
    
        var root = xmlDoc.DocumentElement;
        XmlNode singleNodeResult = root.SelectSingleNode("vehicle[@plate=\"" + licenseTab + "\"]");
        if (singleNodeResult != null)
        {
            info.livesInCali = true;
            info.carMake = singleNodeResult.Attributes["make"].Value;
            info.carModel = singleNodeResult.Attributes["model"].Value;
            info.carColor = singleNodeResult.Attributes["color"].Value;
           
            info.prefLanguage = singleNodeResult.Attributes["owner/preferredLanguage"].Value;
            singleNodeResult = singleNodeResult.SelectSingleNode("owner/name");
            info.personName = singleNodeResult.InnerText;
            singleNodeResult = singleNodeResult.SelectSingleNode("owner/contact");
            info.email = singleNodeResult.InnerText;
        }
        else
        {
            info.livesInCali = false;
     
        }

        //Convert the information from PatientUQMessage into a json string and return it
        JsonSerializerOptions options = new JsonSerializerOptions();
        options.WriteIndented = true;
        String jsonMessageToUQ = JsonSerializer.Serialize(info, options);
        return jsonMessageToUQ;
    }


    // Method to delete a message from a queue
    private static async Task DeleteMessage(
      IAmazonSQS sqsClient, Message message, string qUrl)
    {
        Console.WriteLine($"\nDeleting message {message.MessageId} from queue...");
        await sqsClient.DeleteMessageAsync(qUrl, message.ReceiptHandle);
    }

    // Method to write to log
    public static void WriteToLog(string message)
    {
        string text = String.Format("Date: {0} \t {1}", DateTime.Now, message);
        using (StreamWriter writer = new StreamWriter(logPath, append: true))
        {
            writer.WriteLine(text);
        }
    }

    // Method to get aws credentials for the given profile
    private static AWSCredentials GetAWSCredentialsByName(string profileName)
    {
        if (String.IsNullOrEmpty(profileName))
        {
            throw new ArgumentNullException("profileName cannot be null or empty!");
        }

        SharedCredentialsFile credFile = new SharedCredentialsFile();
        WriteToLog(credFile.FilePath);
        CredentialProfile profile = credFile.ListProfiles().Find(p => p.Name.Equals(profileName));
        if (profile == null)
        {
            throw new Exception(String.Format("Profile named {0} not found", profileName));
        }
        return AWSCredentialsFactory.GetAWSCredentials(profile, new SharedCredentialsFile());
    }


    public Worker()
    {
        InitializeComponent();
    }
}



   