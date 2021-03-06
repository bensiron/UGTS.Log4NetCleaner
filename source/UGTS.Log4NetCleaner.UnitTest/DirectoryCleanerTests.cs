﻿using System;
using System.Collections.Generic;
using System.IO;
using log4net.Appender;
using Moq;
using NUnit.Framework;
using UGTS.Log4NetCleaner.Interfaces;
using UGTS.Testing;

namespace UGTS.Log4NetCleaner.UnitTest
{
    [TestFixture]
    public class DirectoryCleanerTests : TestBase<DirectoryCleaner>
    {
        internal class Clean : DirectoryCleanerTests
        {
            [SetUp]
            public void Setup()
            {
                Mock<IFileSystemOperations>().Setup(x => x.ExistsDirectory(It.IsAny<string>())).Returns(true);
                Mock<IFileSystemOperations>().Setup(x => x.FindFileInfo(It.IsAny<string>(), It.IsAny<Func<string, bool>>()))
                    .Returns(new Dictionary<string, FileInfo>());
            }

            [Test]
            public void Does_Nothing_If_Directory_Does_Not_Exist()
            {
                Mock<IFileSystemOperations>().Setup(x => x.ExistsDirectory(It.IsAny<string>())).Returns(false);

                TestObject.Clean(RandomGenerator.String(), ".txt", DateTime.MaxValue, null);

                Mock<IFileSystemOperations>().Verify(x => x.FindFileInfo(It.IsAny<string>(), It.IsAny<Func<string, bool>>()), Times.Never);
                Mock<IFileSystemOperations>().Verify(x => x.DeleteEmptyDirectories(It.IsAny<string>()), Times.Never);
            }

            [TestCase(true)]
            [TestCase(false)]
            public void Finds_File_Info_Matching_By_Extension(bool extensionStartsWithDot)
            {
                Func<string, bool> actualPredicate = null;
                var extension = "." + RandomGenerator.String();
                var path = RandomGenerator.String();
                Mock<IFileSystemOperations>().Setup(x => x.FindFileInfo(path, It.IsAny<Func<string, bool>>()))
                    .Returns(new Dictionary<string, FileInfo>())
                    .Callback<string, Func<string, bool>>((_, predicate) => actualPredicate = predicate);

                TestObject.Clean(path, extension.Substring(extensionStartsWithDot ? 0 : 1), DateTime.MaxValue, null);

                Mock<IFileSystemOperations>().Verify(x => x.FindFileInfo(path, It.IsAny<Func<string, bool>>()));
                Mock<IFileSystemOperations>().Verify(x => x.FindFileInfo(It.IsAny<string>(), It.IsAny<Func<string, bool>>()), Times.Once);

                Assert.True(actualPredicate("a" + extension));
                Assert.True(actualPredicate("a" + extension.ToUpper()));
                Assert.True(actualPredicate("a" + extension.ToUpper() + ".24243"));
                Assert.True(actualPredicate("a" + extension + ".110"));
                Assert.False(actualPredicate("a"));
                Assert.False(actualPredicate("a" + extension + ".not"));
                Assert.False(actualPredicate(""));
                Assert.False(actualPredicate(null));
                Assert.False(actualPredicate("a." + RandomGenerator.String()));
            }

            [Test]
            public void Finds_File_Info_Matching_Anything_Star_Extension_Specified()
            {
                Func<string, bool> actualPredicate = null;
                var path = RandomGenerator.String();
                Mock<IFileSystemOperations>().Setup(x => x.FindFileInfo(path, It.IsAny<Func<string, bool>>()))
                    .Returns(new Dictionary<string, FileInfo>())
                    .Callback<string, Func<string, bool>>((_, predicate) => actualPredicate = predicate);

                TestObject.Clean(path, "*", DateTime.MaxValue, null);

                Mock<IFileSystemOperations>().Verify(x => x.FindFileInfo(path, It.IsAny<Func<string, bool>>()));
                Mock<IFileSystemOperations>().Verify(x => x.FindFileInfo(It.IsAny<string>(), It.IsAny<Func<string, bool>>()), Times.Once);

                Assert.True(actualPredicate("a.txt"));
                Assert.True(actualPredicate("a.log"));
                Assert.True(actualPredicate("a"));
                Assert.True(actualPredicate("a.110"));
                Assert.False(actualPredicate(""));
                Assert.False(actualPredicate("a\\b\\" + DirectoryCleaner.LastCleaningCheckFileName));
                Assert.False(actualPredicate(DirectoryCleaner.LastCleaningCheckFileName.ToUpper()));
                Assert.False(actualPredicate(null));
            }

            [TestCase(null)]
            [TestCase("")]
            [TestCase(" ")]
            public void Never_Matches_If_FileExtension_Blank(string fileExtension)
            {
                Func<string, bool> actualPredicate = null;
                var path = RandomGenerator.String();
                Mock<IFileSystemOperations>().Setup(x => x.FindFileInfo(path, It.IsAny<Func<string, bool>>()))
                    .Returns(new Dictionary<string, FileInfo>())
                    .Callback<string, Func<string, bool>>((_, predicate) => actualPredicate = predicate);

                TestObject.Clean(path, fileExtension, DateTime.MaxValue, null);

                Mock<IFileSystemOperations>().Verify(x => x.FindFileInfo(path, It.IsAny<Func<string, bool>>()));
                Mock<IFileSystemOperations>().Verify(x => x.FindFileInfo(It.IsAny<string>(), It.IsAny<Func<string, bool>>()), Times.Once);

                Assert.False(actualPredicate("a."));
                Assert.False(actualPredicate("a"));
                Assert.False(actualPredicate("a\\b"));
                Assert.False(actualPredicate(".a"));
                Assert.False(actualPredicate(null));
                Assert.False(actualPredicate(" "));
                Assert.False(actualPredicate(""));
            }

            [Test]
            public void Removes_Files_Older_Than_The_Cutoff_Time()
            {
                var cutoff = RandomGenerator.DateTime();
                var found = new Dictionary<string, FileInfo>
                {
                    ["a"] = new FileInfo { LastWriteTimeUtc = cutoff },
                    ["b"] = new FileInfo { LastWriteTimeUtc = cutoff.AddTicks(-1) }
                };
                Mock<IFileSystemOperations>().Setup(x => x.FindFileInfo(It.IsAny<string>(), It.IsAny<Func<string, bool>>()))
                    .Returns(found);

                TestObject.Clean(RandomGenerator.String(), RandomGenerator.String(), cutoff, null);

                Mock<IFileSystemOperations>().Verify(x => x.DeleteFile("b"));
                Mock<IFileSystemOperations>().Verify(x => x.DeleteFile(It.IsAny<string>()), Times.Once);
            }

            [Test]
            public void Removes_Old_Files_Until_Size_Limit_Satisfied()
            {
                var time = RandomGenerator.DateTime();
                var found = new Dictionary<string, FileInfo>
                {
                    ["a"] = new FileInfo { LastWriteTimeUtc = time, Length = 1000 },
                    ["b"] = new FileInfo { LastWriteTimeUtc = time.AddTicks(-1), Length = 2 },
                    ["c"] = new FileInfo { LastWriteTimeUtc = time.AddTicks(10), Length = 200 },
                    ["d"] = new FileInfo { LastWriteTimeUtc = time.AddTicks(-100), Length = 1000 }
                };
                Mock<IFileSystemOperations>().Setup(x => x.FindFileInfo(It.IsAny<string>(), It.IsAny<Func<string, bool>>()))
                    .Returns(found);

                TestObject.Clean(RandomGenerator.String(), RandomGenerator.String(), null, 1201);

                Mock<IFileSystemOperations>().Verify(x => x.DeleteFile("d"));
                Mock<IFileSystemOperations>().Verify(x => x.DeleteFile("b"));
                Mock<IFileSystemOperations>().Verify(x => x.DeleteFile(It.IsAny<string>()), Times.Exactly(2));
            }

            [TestCase(999)]
            [TestCase(1001)]
            public void Removes_According_To_Both_Limits(long sizeLimit)
            {
                var time = RandomGenerator.DateTime();
                var found = new Dictionary<string, FileInfo>
                {
                    ["b"] = new FileInfo { LastWriteTimeUtc = time.AddTicks(-1), Length = 2 },
                    ["d"] = new FileInfo { LastWriteTimeUtc = time.AddTicks(100), Length = 1000 }
                };
                Mock<IFileSystemOperations>().Setup(x => x.FindFileInfo(It.IsAny<string>(), It.IsAny<Func<string, bool>>()))
                    .Returns(found);

                TestObject.Clean(RandomGenerator.String(), RandomGenerator.String(), time, sizeLimit);

                Mock<IFileSystemOperations>().Verify(x => x.DeleteFile("b"));
                if (sizeLimit <= 1000) Mock<IFileSystemOperations>().Verify(x => x.DeleteFile("d"));
                Mock<IFileSystemOperations>().Verify(x => x.DeleteFile(It.IsAny<string>()), Times.Exactly(sizeLimit <= 1000 ? 2 : 1));
            }

            [Test]
            public void Finally_Removes_Empty_Directories()
            {
                var path = RandomGenerator.String();
                Mock<IFileSystemOperations>().Setup(x => x.DeleteFile(It.IsAny<string>()))
                    .Callback(() => Mock<IFileSystemOperations>().Verify(x => x.DeleteEmptyDirectories(It.IsAny<string>()), Times.Never));

                TestObject.Clean(path, RandomGenerator.String(), RandomGenerator.DateTime(), RandomGenerator.Long());

                Mock<IFileSystemOperations>().Verify(x => x.DeleteEmptyDirectories(path));
                Mock<IFileSystemOperations>().Verify(x => x.DeleteEmptyDirectories(It.IsAny<string>()), Times.Once);
            }
        }

        internal class GetLastCleaningTime : DirectoryCleanerTests
        {
            [Test]
            public void Returns_LastWriteTimeUtc_Of_Cleaning_File_If_Present()
            {
                var path = RandomGenerator.String();
                var expectedTime = RandomGenerator.DateTime();
                var expectedPath = Path.Combine(path, "lastcleaning.check");
                Mock<IFileSystemOperations>().Setup(x => x.GetFileInfo(expectedPath)).Returns(new FileInfo {LastWriteTimeUtc = expectedTime});

                var actual = TestObject.GetLastCleaningTime(path);

                Mock<IFileSystemOperations>().Verify(x => x.GetFileInfo(It.IsAny<string>()), Times.Once);
                Assert.That(actual, Is.EqualTo(expectedTime));
            }

            [Test]
            public void Returns_Null_If_Not_Present()
            {
                var actual = TestObject.GetLastCleaningTime(RandomGenerator.String());

                Assert.Null(actual);
            }
        }

        internal class UpdateLastCleaningTime : DirectoryCleanerTests
        {
            [Test]
            public void Returns_CreateEmptyFile_Result()
            {
                var path = RandomGenerator.String();
                var expectedTime = RandomGenerator.DateTime();
                var expectedPath = Path.Combine(path, "lastcleaning.check");
                Mock<IFileSystemOperations>().Setup(x => x.CreateEmptyFile(expectedPath)).Returns(expectedTime);

                var actual = TestObject.UpdateLastCleaningTime(path);

                Assert.That(actual, Is.EqualTo(expectedTime));
            }

            [Test]
            public void Returns_Now_If_CreateEmptyFile_Returns_Null()
            {
                var now = RandomGenerator.DateTime();
                Mock<RollingFileAppender.IDateTime>().Setup(x => x.Now).Returns(now);

                var actual = TestObject.UpdateLastCleaningTime(RandomGenerator.String());

                Assert.That(actual, Is.EqualTo(now));
            }
        }

        internal class GetFileExtension : DirectoryCleanerTests
        {
            [TestCase("a.txt", ".txt")]
            [TestCase("a.txt.14", ".txt")]
            [TestCase("a.txt.log", ".log")]
            [TestCase("a", "")]
            [TestCase("", null)]
            [TestCase(null, null)]
            public void Returns_File_Extension_Without_Backup_Number(string filename, string expected)
            {
                Assert.That(TestObject.GetFileExtension(filename), Is.EqualTo(expected));
            }
        }
    }
}