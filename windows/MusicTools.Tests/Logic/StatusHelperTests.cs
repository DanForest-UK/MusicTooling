using Microsoft.VisualStudio.TestTools.UnitTesting;
using MusicTools.Logic;
using MusicTools.Core;
using System;
using LanguageExt;
using static MusicTools.Core.Types;

namespace MusicTools.Tests
{
    /// <summary>
    /// Unit tests for the <see cref="StatusHelper"/> class.
    /// </summary>
    [TestClass]
    public class StatusHelperTests
    {
        private string capturedMessage;
        private StatusLevel capturedLevel;
        private bool statusCalled;

        /// <summary>
        /// Initializes the test environment by resetting captured values and mocking runtime methods.
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            capturedMessage = null;
            capturedLevel = StatusLevel.Info;
            statusCalled = false;

            Runtime.Status = (message, level) =>
            {
                capturedMessage = message;
                capturedLevel = level;
                statusCalled = true;
                return Unit.Default;
            };

            Runtime.Info = message => { return Unit.Default; };
            Runtime.Warning = message => { return Unit.Default; };
            Runtime.Error = (message, ex) => { return Unit.Default; };
        }

        /// <summary>
        /// Tests that StatusHelper.Info sends an informational status message.
        /// </summary>
        [TestMethod]
        public void SendInfoMessage()
        {
            var message = "Test info message";

            StatusHelper.Info(message);

            Assert.IsTrue(statusCalled, "Runtime.Status should be called");
            Assert.AreEqual(message, capturedMessage, "Message should be passed through");
            Assert.AreEqual(StatusLevel.Info, capturedLevel, "Level should be Info");
        }

        /// <summary>
        /// Tests that StatusHelper.Warning sends a warning status message.
        /// </summary>
        [TestMethod]
        public void SendWarningMessage()
        {
            var message = "Test warning message";

            StatusHelper.Warning(message);

            Assert.IsTrue(statusCalled, "Runtime.Status should be called");
            Assert.AreEqual(message, capturedMessage, "Message should be passed through");
            Assert.AreEqual(StatusLevel.Warning, capturedLevel, "Level should be Warning");
        }

        /// <summary>
        /// Tests that StatusHelper.Error sends an error status message.
        /// </summary>
        [TestMethod]
        public void SendErrorMessage()
        {
            var message = "Test error message";

            StatusHelper.Error(message);

            Assert.IsTrue(statusCalled, "Runtime.Status should be called");
            Assert.AreEqual(message, capturedMessage, "Message should be passed through");
            Assert.AreEqual(StatusLevel.Error, capturedLevel, "Level should be Error");
        }

        /// <summary>
        /// Tests that StatusHelper.Error sends an error status message with an exception.
        /// </summary>
        [TestMethod]
        public void ErrorWithException()
        {
            var message = "Test error message with exception";
            var exception = new Exception("Test exception");

            StatusHelper.Error(message, exception);

            Assert.IsTrue(statusCalled, "Runtime.Status should be called");
            Assert.AreEqual(message, capturedMessage, "Message should be passed through");
            Assert.AreEqual(StatusLevel.Error, capturedLevel, "Level should be Error");
        }

        /// <summary>
        /// Tests that StatusHelper.SendStatus sends a custom status message with the specified level.
        /// </summary>
        [TestMethod]
        public void SendStatusMessageAndLevel()
        {
            var message = "Test custom status message";
            var level = StatusLevel.Success;

            StatusHelper.SendStatus(message, level);

            Assert.IsTrue(statusCalled, "Runtime.Status should be called");
            Assert.AreEqual(message, capturedMessage, "Message should be passed through");
            Assert.AreEqual(level, capturedLevel, "Level should be passed through");
        }
    }
}