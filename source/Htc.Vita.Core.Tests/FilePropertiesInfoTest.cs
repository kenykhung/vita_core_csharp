﻿using System.IO;
using Htc.Vita.Core.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace Htc.Vita.Core.Tests
{
    public class FilePropertiesInfoTest
    {
        private readonly ITestOutputHelper _output;

        public FilePropertiesInfoTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Default_0_GetPropertiesInfo()
        {
            if (!Runtime.Platform.IsWindows)
            {
                return;
            }
            var fileInfo = new FileInfo("C:\\Windows\\SysWOW64\\msvcp120.dll");
            if (!fileInfo.Exists)
            {
                fileInfo = new FileInfo("C:\\Windows\\System32\\msvcp120.dll");
            }
            if (!fileInfo.Exists)
            {
                fileInfo = new FileInfo("C:\\Windows\\System32\\svchost.exe");
            }
            Assert.True(fileInfo.Exists);
            var filePropertiesInfo = FilePropertiesInfo.GetPropertiesInfo(fileInfo);
            Assert.NotNull(filePropertiesInfo);
            Assert.True(!string.IsNullOrEmpty(filePropertiesInfo.IssuerDistinguishedName));
            Assert.Contains("O=", filePropertiesInfo.IssuerDistinguishedName);
            Assert.True(!string.IsNullOrEmpty(filePropertiesInfo.IssuerName));
            Assert.True(!string.IsNullOrEmpty(filePropertiesInfo.ProductVersion));
            Assert.True(!string.IsNullOrEmpty(filePropertiesInfo.SubjectDistinguishedName));
            Assert.Contains("O=", filePropertiesInfo.SubjectDistinguishedName);
            Assert.True(!string.IsNullOrEmpty(filePropertiesInfo.SubjectName));
            Assert.True(!string.IsNullOrEmpty(filePropertiesInfo.PublicKey));
            Assert.True(filePropertiesInfo.Verified);
            Assert.NotEmpty(filePropertiesInfo.TimestampList);
            var index = 0;
            foreach (var time in filePropertiesInfo.TimestampList)
            {
                _output.WriteLine("filePropertiesInfo.TimestampList[{0}]: {1}", index, time);
                index++;
            }
            Assert.True(!string.IsNullOrEmpty(filePropertiesInfo.Version));
        }

        [Fact]
        public void Default_0_GetPropertiesInfo_WithNotepad()
        {
            if (!Runtime.Platform.IsWindows)
            {
                return;
            }
            var fileInfo = new FileInfo("C:\\Windows\\System32\\notepad.exe");
            Assert.True(fileInfo.Exists);
            var filePropertiesInfo = FilePropertiesInfo.GetPropertiesInfo(fileInfo);
            Assert.NotNull(filePropertiesInfo);
            Assert.True(string.IsNullOrEmpty(filePropertiesInfo.IssuerDistinguishedName));
            Assert.True(string.IsNullOrEmpty(filePropertiesInfo.IssuerName));
            _output.WriteLine("filePropertiesInfo.ProductVersion: " + filePropertiesInfo.ProductVersion);
            Assert.True(string.IsNullOrEmpty(filePropertiesInfo.SubjectDistinguishedName));
            Assert.True(string.IsNullOrEmpty(filePropertiesInfo.SubjectName));
            Assert.True(string.IsNullOrEmpty(filePropertiesInfo.PublicKey));
            Assert.False(filePropertiesInfo.Verified);
            Assert.Empty(filePropertiesInfo.TimestampList);
            _output.WriteLine("filePropertiesInfo.Version: " + filePropertiesInfo.Version);
        }
    }
}
