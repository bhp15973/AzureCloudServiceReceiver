using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace WorkerRoleWithSBQueue1
{
    public class WorkerRole : RoleEntryPoint
    {
        // QueueClient is thread-safe. Recommended that you cache 
        // rather than recreating it on every request
        SubscriptionClient Client;
        ManualResetEvent CompletedEvent = new ManualResetEvent(false);

        public override void Run()
        {
            Trace.WriteLine("Starting processing of messages");

            // Initiates the message pump and callback is invoked for each message that is received, calling close on the client will stop the pump.
            Client.OnMessage((receivedMessage) =>
                {
                    try
                    {
                        // Process the message
                        Trace.WriteLine("Processing Service Bus message: " + receivedMessage.SequenceNumber.ToString());
                        receivedMessage.Complete();
                    }
                    catch
                    {
                        // Handle any message processing specific exceptions here
                    }
                }, new OnMessageOptions()  );

            CompletedEvent.WaitOne();
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            // Create the queue if it does not exist already
            string connectionString = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString");
            string topicName = CloudConfigurationManager.GetSetting("TopicName");
            string subscriptionName = CloudConfigurationManager.GetSetting("SubscriptionName");
            var namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);
            if (!namespaceManager.SubscriptionExists(topicName, subscriptionName))
            {
                namespaceManager.CreateSubscription(topicName, subscriptionName);
            }

            // Initialize the connection to Service Bus Queue
            Client = SubscriptionClient.CreateFromConnectionString(connectionString, topicName, subscriptionName);
            return base.OnStart();
        }

        public override void OnStop()
        {
            // Close the connection to Service Bus Queue
            Client.Close();
            CompletedEvent.Set();
            base.OnStop();
        }
    }
}
