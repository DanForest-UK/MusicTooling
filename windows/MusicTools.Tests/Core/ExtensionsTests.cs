using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using LanguageExt;
using static LanguageExt.Prelude;
using MusicTools.Domain;

namespace MusicTools.Tests
{
    [TestClass]
    public class ExtensionsTests
    {
        /// <summary>
        /// Tests that ValueOrNone returns Some for a valid string
        /// </summary>
        [TestMethod]
        public void ValueOrNone()
        {
            var testString = "Valid String";
            var result = testString.ValueOrNone();
            Assert.IsTrue(result.IsSome, "Result should be Some for valid string");
            result.IfSome(value => Assert.AreEqual(testString, value, "Value should match input"));
        }

        /// <summary>
        /// Tests that ValueOrNone returns None for null, empty, or whitespace strings
        /// </summary>
        [TestMethod]
        public void ValueOrNoneEmpty()
        {
            string nullString = null;
            var emptyString = "";
            var whitespaceString = "   ";

            var resultNull = nullString.ValueOrNone();
            var resultEmpty = emptyString.ValueOrNone();
            var resultWhitespace = whitespaceString.ValueOrNone();

            Assert.IsTrue(resultNull.IsNone, "Result should be None for null string");
            Assert.IsTrue(resultEmpty.IsNone, "Result should be None for empty string");
            Assert.IsTrue(resultWhitespace.IsNone, "Result should be None for whitespace string");
        }
              
        /// <summary>
        /// Tests that ValueOrNone for dictionaries returns Some for existing keys and None for non-existing keys
        /// </summary>
        [TestMethod]
        public void DictionaryValueOrNone()
        {
            var dictionary = new Dictionary<int, string>
            {
                { 1, "Value 1" },
                { 2, "Value 2" }
            };

            var existingResult = dictionary.ValueOrNone(1);
            var nonExistingResult = dictionary.ValueOrNone(3);

            Assert.IsTrue(existingResult.IsSome, "Result should be Some for existing key");
            existingResult.IfSome(value => Assert.AreEqual("Value 1", value, "Value should match dictionary value"));

            Assert.IsTrue(nonExistingResult.IsNone, "Result should be None for non-existing key");
        }

        /// <summary>
        /// Tests that HasValue correctly identifies strings with value and strings without value
        /// </summary>
        [TestMethod]
        public void HasValue()
        {
            string validString = "Valid String";
            string nullString = null;
            string emptyString = "";
            string whitespaceString = "   ";

            bool validResult = validString.HasValue();
            bool nullResult = nullString.HasValue();
            bool emptyResult = emptyString.HasValue();
            bool whitespaceResult = whitespaceString.HasValue();

            Assert.IsTrue(validResult, "Valid string should return true");
            Assert.IsFalse(nullResult, "Null string should return false");
            Assert.IsFalse(emptyResult, "Empty string should return false");
            Assert.IsFalse(whitespaceResult, "Whitespace string should return false");
        }

        /// <summary>
        /// Tests that IfNoneThrow returns the value when the option is Some
        /// </summary>
        [TestMethod]
        public void IfNoneThrow()
        {
            var someOption = Some(42);
            var result = someOption.IfNoneThrow();
            Assert.AreEqual(42, result, "IfNoneThrow should return the value for Some");
        }

        /// <summary>
        /// Tests that IfNoneThrow throws an exception when the option is None
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(Exception), "Option was none")]
        public void IfNoneThrowThrows()
        {
            var noneOption = Option<int>.None;
            noneOption.IfNoneThrow();
        }

        /// <summary>
        /// Tests that IfNoneThrow throws an exception with a custom message when specified
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(Exception), "Custom error message")]
        public void IfNoneThrowCustomErrorMessage()
        {
            var noneOption = Option<int>.None;
            var customMessage = Option<string>.Some("Custom error message");
            noneOption.IfNoneThrow(customMessage);
        }

        /// <summary>
        /// Tests that ToBatchArray correctly splits an array into batches of specified size
        /// </summary>
        [TestMethod]
        public void ToBatchArray()
        {
            var array = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            int batchSize = 3;

            var batches = array.ToBatchArray(batchSize).ToArray();

            Assert.AreEqual(4, batches.Length, "Should return 4 batches for array of 10 with batch size 3");

            Assert.AreEqual(3, batches[0].Length, "First batch should have 3 items");
            Assert.AreEqual(1, batches[0][0], "First batch, first item should be 1");
            Assert.AreEqual(2, batches[0][1], "First batch, second item should be 2");
            Assert.AreEqual(3, batches[0][2], "First batch, third item should be 3");

            Assert.AreEqual(3, batches[1].Length, "Second batch should have 3 items");
            Assert.AreEqual(4, batches[1][0], "Second batch, first item should be 4");

            Assert.AreEqual(3, batches[2].Length, "Third batch should have 3 items");
            Assert.AreEqual(7, batches[2][0], "Third batch, first item should be 7");

            Assert.AreEqual(1, batches[3].Length, "Fourth batch should have 1 item");
            Assert.AreEqual(10, batches[3][0], "Fourth batch, first item should be 10");
        }       
    }
}