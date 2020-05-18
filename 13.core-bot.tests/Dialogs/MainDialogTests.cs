// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CoreBot.Tests.Common;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Testing;
using Microsoft.Bot.Builder.Testing.XUnit;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.BotBuilderSamples;
using Microsoft.BotBuilderSamples.Dialogs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace CoreBot.Tests.Dialogs
{
    public class MainDialogTests : BotTestBase
    {
        private readonly BookingDialog _mockBookingDialog;
        private readonly Mock<ILogger<MainDialog>> _mockLogger;

        public MainDialogTests(ITestOutputHelper output)
            : base(output)
        {
            _mockLogger = new Mock<ILogger<MainDialog>>();
            var expectedBookingDialogResult = new BookingDetails()
            {
                Destination = "Seattle",
                Origin = "New York",
                TravelDate = $"{DateTime.UtcNow.AddDays(1):yyyy-MM-dd}"
            };
            _mockBookingDialog = SimpleMockFactory.CreateMockDialog<BookingDialog>(expectedBookingDialogResult).Object;
        }

     
        //
        [Theory]
        [InlineData("I want to take the COVID-19 Self Assessment Test", "None", "Please call 911 or go directly to your nearest emergency department.", "I have you booked to Seattle from New York")]
        public async Task TaskSelector(string utterance, string intent, string invokedDialogResponse, string taskConfirmationMessage)
        {
            // Create a mock recognizer that returns the expected intent.
            var mockLuisRecognizer = SimpleMockFactory.CreateMockLuisRecognizer<FlightBookingRecognizer, FlightBooking>(
                new FlightBooking
                {
                    Intents = new Dictionary<FlightBooking.Intent, IntentScore>
                    {
                        { Enum.Parse<FlightBooking.Intent>(intent), new IntentScore() { Score = 1 } },
                    },
                    Entities = new FlightBooking._Entities(),
                },
                new Mock<IConfiguration>().Object);
            mockLuisRecognizer.Setup(x => x.IsConfigured).Returns(true);
            var sut = new MainDialog(mockLuisRecognizer.Object, _mockBookingDialog, _mockLogger.Object);
            var testClient = new DialogTestClient(Channels.Test, sut, middlewares: new[] { new XUnitDialogTestLogger(Output) });

            // Execute the test case
            Output.WriteLine($"Test Case: {intent}");
            var reply = await testClient.SendActivityAsync<IMessageActivity>("hi");
            Assert.Equal("Are you experiencing any of the following: severe difficulty breathing, chest pain, very hard time waking up, confusion, lost consciousness?", reply.Text);

            

            reply = await testClient.SendActivityAsync<IMessageActivity>(utterance);
            Assert.Equal("Please call 911 or go directly to your nearest emergency department.", reply.Text);

            

            // Validate that the MainDialog starts over once the task is completed.
            reply = testClient.GetNextReply<IMessageActivity>();
            //Assert.Equal("What else can I do for you?", reply.Text);
        }


        /// <summary>
        /// Loads the embedded json resource with the LUIS as a string.
        /// </summary>
        private string GetEmbeddedTestData(string resourceName)
        {
            using (var stream = GetType().Assembly.GetManifestResourceStream(resourceName))
            {
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}
