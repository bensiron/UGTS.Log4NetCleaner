using System;
using Moq;
using NUnit.Framework;
using UGTS.Testing;
using UGTS.Log4Net.Extensions.Interfaces;

namespace UGTS.Log4Net.Extensions.UnitTest
{
    internal class FileSystemOperationsTests : TestBase<FileSystemOperations, IFileSystemOperations>
    {
        private Mock<IFileSystem> _fs;

        protected override FileSystemOperations CreateTestObject()
        {
            var t = new FileSystemOperations(DefineMock<IFileSystem>().Object);
            _fs = Mock<IFileSystem>();
            return t;
        }

        internal class ExistsDirectory : FileSystemOperationsTests
        {
            [TestCase(true)]
            [TestCase(false)]
            public void Returns_Filesystem_Result(bool expected)
            {
                var path = RandomGenerator.String();
                _fs.Setup(x => x.ExistsDirectory(It.IsAny<string>())).Returns(expected);

                var actual = TestObject.ExistsDirectory(path);

                Assert.That(actual, Is.EqualTo(expected));
                _fs.Verify(x => x.ExistsDirectory(path));
                _fs.Verify(x => x.ExistsDirectory(It.IsAny<string>()), Times.Once);
            }
        }

        internal class FindFileInfo : FileSystemOperationsTests
        {
            [Test]
            public void Returns_Flattened_File_Info()
            {
                var info1 = new FileInfo();
                var info2 = new FileInfo();
                var info3 = new FileInfo();
                var info4 = new FileInfo();
                var exclude = RandomGenerator.String();
                var nonMatchingFile = $"a\\{exclude}.log";

                _fs.Setup(x => x.GetDirectories("a")).Returns(new[] { "a\\b", "a\\c" });
                _fs.Setup(x => x.GetDirectories("a\\b")).Returns(new[] { "a\\b\\d" });
                _fs.Setup(x => x.GetDirectories("a\\c")).Returns(new string[0]);
                _fs.Setup(x => x.GetDirectories("a\\b\\d")).Returns(new string[0]);
                _fs.Setup(x => x.GetFiles("a")).Returns(new[] { "a\\1.txt", "a\\2.txt", nonMatchingFile });
                _fs.Setup(x => x.GetFiles("a\\b")).Returns(new string[0]);
                _fs.Setup(x => x.GetFiles("a\\c")).Returns(new[] { "a\\c\\3.txt" });
                _fs.Setup(x => x.GetFiles("a\\b\\d")).Returns(new[] { "a\\b\\d\\4.txt" });
                _fs.Setup(x => x.GetFileInfo("a\\1.txt")).Returns(info1);
                _fs.Setup(x => x.GetFileInfo("a\\2.txt")).Returns(info2);
                _fs.Setup(x => x.GetFileInfo("a\\c\\3.txt")).Returns(info3);
                _fs.Setup(x => x.GetFileInfo("a\\b\\d\\4.txt")).Returns(info4);
                
                var result = TestObject.FindFileInfo("a", path => !path.Contains(exclude));

                Assert.That(result.Count, Is.EqualTo(4));
                Assert.That(result["a\\1.txt"], Is.SameAs(info1));
                Assert.That(result["a\\2.txt"], Is.SameAs(info2));
                Assert.That(result["a\\c\\3.txt"], Is.SameAs(info3));
                Assert.That(result["a\\b\\d\\4.txt"], Is.SameAs(info4));
                _fs.Verify(x => x.GetFileInfo(nonMatchingFile), Times.Never);
                _fs.Verify(x => x.GetFileInfo(It.IsAny<string>()), Times.Exactly(4));
            }
        }

        internal class CreatEmptyFile : FileSystemOperationsTests
        {
            [Test]
            public void Creates_Empty_File_And_Then_Returns_LastWriteTimeUtc()
            {
                var path = RandomGenerator.String();
                var time = RandomGenerator.DateTime();
                _fs.Setup(x => x.GetFileInfo(path)).Returns(new FileInfo {LastWriteTimeUtc = time})
                    .Callback(() => _fs.Verify(x => x.CreateEmptyFile(path), Times.Once));

                var actual = TestObject.CreateEmptyFile(path);

                _fs.Verify(x => x.CreateEmptyFile(path));
                _fs.Verify(x => x.CreateEmptyFile(It.IsAny<string>()), Times.Once);
                _fs.Verify(x => x.GetFileInfo(It.IsAny<string>()), Times.Once);
                Assert.That(actual, Is.EqualTo(time));
            }

            [Test]
            public void Returns_Null_If_Create_Throws()
            {
                _fs.Setup(x => x.CreateEmptyFile(It.IsAny<string>())).Throws(new Exception());
                _fs.Setup(x => x.GetFileInfo(It.IsAny<string>()))
                    .Returns(new FileInfo {LastWriteTimeUtc = RandomGenerator.DateTime()});

                Assert.Null(TestObject.CreateEmptyFile(RandomGenerator.String()));
            }

            [Test]
            public void Returns_Null_If_GetFileInfo_Throws()
            {
                _fs.Setup(x => x.GetFileInfo(It.IsAny<string>())).Throws(new Exception());

                Assert.Null(TestObject.CreateEmptyFile(RandomGenerator.String()));
            }
        }

        internal class GetFileInfo : FileSystemOperationsTests
        {
            [Test]
            public void Returns_File_Info()
            {
                var path = RandomGenerator.String();
                var info = new FileInfo();
                _fs.Setup(x => x.GetFileInfo(path)).Returns(info);

                var actual = TestObject.GetFileInfo(path);

                Assert.That(actual, Is.SameAs(info));
                _fs.Verify(x => x.GetFileInfo(It.IsAny<string>()), Times.Once);
            }

            [Test]
            public void Returns_Null_If_GetFileInfo_Throws()
            {
                _fs.Setup(x => x.GetFileInfo(It.IsAny<string>())).Throws(new Exception());

                Assert.Null(TestObject.GetFileInfo(RandomGenerator.String()));
            }
        }

        internal class DeleteFile : FileSystemOperationsTests
        {
            [Test]
            public void Calls_Filesystem_Delete_File()
            {
                var path = RandomGenerator.String();
                
                TestObject.DeleteFile(path);
                
                _fs.Verify(x => x.DeleteFile(path));
                _fs.Verify(x => x.DeleteFile(It.IsAny<string>()), Times.Once);
            }

            [Test]
            public void Ignores_Errors()
            {
                _fs.Setup(x => x.DeleteFile(It.IsAny<string>())).Throws(new Exception());

                TestObject.DeleteFile(RandomGenerator.String());
            }
        }

        internal class DeleteEmptyDirectories : FileSystemOperationsTests
        {
            [Test]
            public void Deletes_Empty_Directories()
            {
                _fs.Setup(x => x.GetDirectories("a")).Returns(new[] { "a\\b", "a\\c" });
                _fs.Setup(x => x.GetDirectories("a\\b")).Returns(new[] { "a\\b\\d" });
                _fs.Setup(x => x.GetDirectories("a\\c")).Returns(new string[0]);
                _fs.Setup(x => x.GetDirectories("a\\b\\d")).Returns(new string[0]);
                _fs.Setup(x => x.GetFiles("a")).Returns(new[] { "a\\1.txt", "a\\2.txt", "a\\g.log" });
                _fs.Setup(x => x.GetFiles("a\\b")).Returns(new string[0]);
                _fs.Setup(x => x.GetFiles("a\\c")).Returns(new[] { "a\\c\\3.txt" });
                _fs.Setup(x => x.GetFiles("a\\b\\d")).Returns(new string[0]);

                TestObject.DeleteEmptyDirectories("a");

                _fs.Verify(x => x.DeleteDirectory("a\\b"));
                _fs.Verify(x => x.DeleteDirectory("a\\b\\d"));
                _fs.Verify(x => x.DeleteDirectory(It.IsAny<string>()), Times.Exactly(2));
            }

            [Test]
            public void Continues_In_Case_Of_Error()
            {
                _fs.Setup(x => x.GetDirectories("a")).Returns(new[] { "a\\b", "a\\c", "a\\d" });
                _fs.Setup(x => x.GetDirectories("a\\b")).Returns(new string[0]);
                _fs.Setup(x => x.GetDirectories("a\\c")).Returns(new string[0]);
                _fs.Setup(x => x.GetDirectories("a\\d")).Returns(new string[0]);
                _fs.Setup(x => x.GetFiles("a")).Returns(new[] { "a\\1.txt", "a\\2.txt" });
                _fs.Setup(x => x.GetFiles("a\\b")).Returns(new string[0]);
                _fs.Setup(x => x.GetFiles("a\\c")).Returns(new[] { "a\\c\\3.txt", "a\\c\\4.txt" });
                _fs.Setup(x => x.GetFiles("a\\d")).Returns(new string[0]);
                _fs.Setup(x => x.DeleteDirectory("a\\b")).Throws(new Exception());
            
                TestObject.DeleteEmptyDirectories("a");

                _fs.Verify(x => x.DeleteDirectory("a\\b"));
                _fs.Verify(x => x.DeleteDirectory("a\\d"));
                _fs.Verify(x => x.DeleteDirectory(It.IsAny<string>()), Times.Exactly(2));
            }
        }
    }
}