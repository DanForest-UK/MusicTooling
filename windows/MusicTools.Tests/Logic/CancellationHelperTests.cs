using Microsoft.VisualStudio.TestTools.UnitTesting;
using MusicTools.Logic;
using System.Threading;
using System.Threading.Tasks;
using static LanguageExt.Prelude;

namespace MusicTools.Tests
{
    [TestClass]
    public class CancellationHelperTests
    {
        private string lastInfoMessage;
        private string lastWarningMessage;

        /// <summary>
        /// Sets up mock loggers before each test
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            lastInfoMessage = null;
            lastWarningMessage = null;

            Runtime.Info = message => { lastInfoMessage = message; return unit; };
            Runtime.Warning = message => { lastWarningMessage = message; return unit; };
            Runtime.Error = (message, ex) => { return unit; };
        }

        /// <summary>
        /// Verifies that CheckForCancel throws a TaskCanceledException when the token is cancelled
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(TaskCanceledException))]
        public void CancelThrowsException()
        {
            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;
            tokenSource.Cancel();
            string operationName = "Test Operation";
            CancellationHelper.CheckForCancel(token, operationName);
        }

        /// <summary>
        /// Verifies that CheckForCancel logs an information message when cancellation is requested
        /// </summary>
        [TestMethod]
        public void CancelLogsInfo()
        {
            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;
            tokenSource.Cancel();
            string operationName = "Test Operation";

            try
            {
                CancellationHelper.CheckForCancel(token, operationName);
            }
            catch (TaskCanceledException)
            {
                // Expected exception, ignore
            }

            Assert.IsNotNull(lastInfoMessage, "Info message should be logged");
            StringAssert.Contains(lastInfoMessage, "Test Operation", "Info message should include operation name");
            StringAssert.Contains(lastInfoMessage, "was cancelled", "Info message should indicate cancellation");
        }

        /// <summary>
        /// Verifies that CheckForCancel uses a generic cancellation message when no operation name is provided
        /// </summary>
        [TestMethod]
        public void CancelGenericMessage()
        {
            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;
            tokenSource.Cancel();

            try
            {
                CancellationHelper.CheckForCancel(token);
            }
            catch (TaskCanceledException ex)
            {
                Assert.AreEqual("Operation was cancelled by user", ex.Message, "Generic message should be used when no operation name provided");
                return;
            }

            Assert.Fail("Should have thrown TaskCanceledException");
        }

        /// <summary>
        /// Verifies that CheckForCancel allows execution to continue when cancellation is not requested
        /// </summary>
        [TestMethod]
        public void CheckNoCancelAllowsExecution()
        {
            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;
            bool codeAfterCheckExecuted = false;

            CancellationHelper.CheckForCancel(token, "Test Operation");
            codeAfterCheckExecuted = true;

            Assert.IsTrue(codeAfterCheckExecuted, "Code after check should be executed");
        }

        /// <summary>
        /// Verifies that ResetCancellationToken disposes the existing token source and creates a new one
        /// </summary>
        [TestMethod]
        public void ResetCancellationToken()
        {
            CancellationTokenSource originalCts = new CancellationTokenSource();
            CancellationToken originalToken = originalCts.Token;
            var originalTokenHash = originalCts.GetHashCode();

            var newCts = CancellationHelper.ResetCancellationToken(ref originalCts);
            var newTokenHash = originalCts.GetHashCode();

            Assert.IsNotNull(newCts, "New CancellationTokenSource should be created");
            Assert.AreNotEqual(originalTokenHash, newTokenHash, "New CancellationTokenSource should be different");
            Assert.AreSame(originalCts, newCts, "Reference should be updated and returned");
        }

        /// <summary>
        /// Verifies that ResetCancellationToken properly handles a null CancellationTokenSource
        /// </summary>
        [TestMethod]
        public void NullCancellationSource()
        {
            CancellationTokenSource cts = null;

            var newCts = CancellationHelper.ResetCancellationToken(ref cts);

            Assert.IsNotNull(newCts, "New CancellationTokenSource should be created from null");
            Assert.AreSame(cts, newCts, "Reference should be updated and returned");
        }

        /// <summary>
        /// Verifies that ResetCancellationToken handles exceptions when disposing the token source
        /// </summary>
        [TestMethod]
        public void DisposeToken()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.Cancel(); // Not causing an exception, but exercising some logic

            // Now dispose it manually, so the helper might have an issue
            cts.Dispose();

            bool exceptionThrown = false;
            CancellationTokenSource newCts = null;

            try
            {
                newCts = CancellationHelper.ResetCancellationToken(ref cts);
            }
            catch
            {
                exceptionThrown = true;
            }

            Assert.IsFalse(exceptionThrown, "No exception should be thrown even with already disposed CTS");
            Assert.IsNotNull(newCts, "New CancellationTokenSource should be created");
        }

        /// <summary>
        /// Verifies that CancelOperation logs an information message when the operation name is provided
        /// </summary>
        [TestMethod]
        public void CancelOperationMessages()
        {
            var cts = new CancellationTokenSource();
            string operationName = "Test Operation";

            bool result = CancellationHelper.CancelOperation(cts, operationName);

            Assert.IsTrue(result, "CancelOperation should return true when successful");
            Assert.IsNotNull(lastInfoMessage, "Info message should be logged");
            StringAssert.Contains(lastInfoMessage, operationName, "Info message should include operation name");
            StringAssert.Contains(lastInfoMessage, "cancelled", "Info message should indicate cancellation");
        }            

        /// <summary>
        /// Verifies that CancelOperation returns false when the token has already been cancelled
        /// </summary>
        [TestMethod]
        public void CancelAlreadyCancelledOperation()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Already cancelled

            bool result = CancellationHelper.CancelOperation(cts, "Test Operation");

            Assert.IsFalse(result, "CancelOperation should return false for already cancelled CTS");
        }

        /// <summary>
        /// Verifies that CancelOperation logs errors and returns false when an exception occurs
        /// </summary>
        [TestMethod]
        public void CancelWithDisposedLogging()
        {
            string errorMessage = null;
            Runtime.Error = (message, ex) => { errorMessage = message; return unit; };

            var cts = new CancellationTokenSource();
            cts.Dispose(); // This will cause Cancel() to throw

            bool result = CancellationHelper.CancelOperation(cts, "Test Operation");

            Assert.IsFalse(result, "CancelOperation should return false when exception occurs");
            Assert.IsNotNull(errorMessage, "Error message should be logged");
            StringAssert.Contains(errorMessage, "Error cancelling", "Error message should indicate cancellation error");
            StringAssert.Contains(errorMessage, "Test Operation", "Error message should include operation name");
        }
    }
}