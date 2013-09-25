using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
namespace IniUtil.Tests
{
    [TestClass()]
    public class IniFileTests
    {
        [TestMethod()]
        [ExpectedException(typeof(InvalidDataException))]
        public void AbortModeTest()
        {
            var ini = new IniFile()
            {
                DuplicatedKeyNameMode = DuplicatedKeyNameMode.Abort
            };
            ini.LoadString(MakeIniString(
                "key1=val1",
                "key2=val2",
                "key1=val3"));
            Assert.Fail("Exception should be occurred because duplicate key is prohibited.");
        }

        [TestMethod]
        public void IgnoreModeTest()
        {
            var ini = new IniFile()
            {
                DuplicatedKeyNameMode = DuplicatedKeyNameMode.Ignore
            };
            ini.LoadString(MakeIniString(
                "key1=val1",
                "key2=val2",
                "key1=val3"));
            Assert.AreEqual(2, ini["global"].Properties.Count);
        }

        [TestMethod]
        public void AllowModeTest()
        {
            var ini = new IniFile()
            {
                DuplicatedKeyNameMode = DuplicatedKeyNameMode.Allow
            };
            ini.LoadString(MakeIniString(
                "key1=val1",
                "key2=val2",
                "key1=val3"));
            Assert.AreEqual(2, ini["global"].Properties.Count);
            Assert.AreEqual("val1;val3", ini["global"].Properties["key1"]);
        }

        [TestMethod]
        public void LineContinuationTest()
        {
            var ini = new IniFile()
            {
                AllowLineContinuation = true
            };
            ini.LoadString(MakeIniString(
                "key1=val1    \\",
                "    with line2\\",
                "    with line3    \\",
                "    with line4",
                "key2=val2",
                "key3=val3\\",
                "    with line7"));
            Assert.AreEqual(3, ini["global"].Properties.Count);
            Assert.AreEqual("val1    with line2    with line3    with line4", ini["global"].Properties["key1"]);
            Assert.AreEqual("val2", ini["global"].Properties["key2"]);
            Assert.AreEqual("val3    with line7", ini["global"].Properties["key3"]);
        }

        [TestMethod]
        public void CommentCharsTest()
        {
            var ini = new IniFile()
            {
                CommentChars = new[] { ';', '#' }
            };
            ini.LoadString(MakeIniString(
                ";comment1",
                "#comment2",
                "key1=val1"));
            Assert.AreEqual(1, ini["global"].Properties.Count);
        }

        [TestMethod]
        public void InlineCommentTest()
        {
            var ini = new IniFile()
            {
                AllowInlineComment = true
            };
            ini.LoadString(MakeIniString(
                "key1=val1 ; this is an inline comment",
                "key2=val2"));
            Assert.AreEqual(2, ini["global"].Properties.Count);
            Assert.AreEqual("val1", ini["global"].Properties["key1"]);
        }

        [TestMethod]
        public void InlineCommentWithLineContinuationTest()
        {
            var ini = new IniFile()
            {
                AllowInlineComment = true,
                AllowLineContinuation = true
            };
            ini.LoadString(MakeIniString(
                "key1=val1   ; comment1\\",
                " with line2 ; comment2"));
            Assert.AreEqual("val1 with line2", ini["global"].Properties["key1"]);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void NotExistSectionTest()
        {
            var ini = new IniFile();
            ini.LoadString(MakeIniString(
                "key1=val1"));
            var section = ini["InvalidSectionName"];
            Assert.Fail("Exception must be occured when accessing not-exist section.");
        }

        [TestMethod]
        public void NotExistSectionWithoutExceptionTest()
        {
            var ini = new IniFile();
            ini.LoadString(MakeIniString(
                "key1=val1"));
            var section = ini.Find("InvalidSectionName");
            Assert.IsNull(section);
        }

        [TestMethod]
        public void EmptyContentsTest()
        {
            var ini = new IniFile();
            ini.LoadString("");
            Assert.AreEqual(0, ini["global"].Properties.Count);
        }

        [TestMethod]
        public void EmptyContentsWithCommentsTest()
        {
            var ini = new IniFile();
            ini.LoadString(MakeIniString(
                "; all",
                "; lines",
                "; are",
                "; commented",
                "; out"));
            Assert.AreEqual(0, ini["global"].Properties.Count);
        }

        [TestMethod]
        public void MultipleSectionsTest()
        {
            var ini = new IniFile();
            ini.LoadString(MakeIniString(
                "[Section1]",
                "key1=val1",
                "[Section2]",
                "key1=val1"));
            Assert.AreEqual(3, ini.Sections.Count);
            Assert.AreEqual("val1", ini["Section1"]["key1"]);
            Assert.AreEqual("val1", ini["Section2"]["key1"]);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public void DuplicateKeyNameInSectionTest()
        {
            var ini = new IniFile()
            {
                DuplicatedKeyNameMode = DuplicatedKeyNameMode.Abort
            };
            ini.LoadString(MakeIniString(
                "[Section1]",
                "key1=val1",
                "key1=val2"));
            Assert.Fail("Exception should be occurred because duplicated key name is not allowed.");
        }


        private string MakeIniString(params string[] lines)
        {
            return string.Join(Environment.NewLine, lines);
        }
    }
}
