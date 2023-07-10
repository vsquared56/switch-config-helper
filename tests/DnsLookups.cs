using System.Net.Sockets;
using Xunit;
using FluentAssertions;

namespace SwitchConfigHelper.Tests
{
    public class DnsLookupsTests
    {
        public class ResolveATests
        {
            [Fact]
            public void ResolveATestLocalhost()
            {
                var hostname = "localhost";
                var expectedResult = "127.0.0.1";
                var result = SwitchConfigHelper.DnsLookups.ResolveA(hostname);
                Assert.Equal(expectedResult, result);
            }

            [Fact]
            public void ResolveATestGoogleDns()
            {
                var hostname = "dns.google"; //Should have A records for 8.8.8.8 and 8.8.4.4
                Action testWrite = () => SwitchConfigHelper.DnsLookups.ResolveA(hostname);
                Assert.Throws<ArgumentException>(testWrite); //ResolveA expects only a single A record
            }

            [Fact]
            public void ResolveATestNonexistentDns()
            {
                var hostname = "nonexistent.example.com"; //Should not resolve
                Action testWrite = () => SwitchConfigHelper.DnsLookups.ResolveA(hostname);
                Assert.Throws<SocketException>(testWrite);
            }

            [Fact]
            public void ResolveATestExample()
            {
                var hostname = "www.example.com";
                //Don't check the actual resolved hostname
                var result = SwitchConfigHelper.DnsLookups.ResolveA(hostname);
            }
        }

		public class ResolveSingleATests
        {
            [Fact]
            public void ResolveSingleATestLocalhost()
            {
                var hostname = "localhost";
                var expectedResult = "127.0.0.1";
                var result = SwitchConfigHelper.DnsLookups.ResolveSingleA(hostname);
                Assert.Equal(expectedResult, result);
            }

            [Fact]
            public void ResolveSingleATestGoogleDns()
            {
                var hostname = "dns.google"; //Should have A records for 8.8.8.8 and 8.8.4.4
                var expectedResult = "8.8.4.4"; //Should always resolve the first record numerically
                var result = SwitchConfigHelper.DnsLookups.ResolveSingleA(hostname);
                Assert.Equal(expectedResult, result);
            }

            [Fact]
            public void ResolveSingleATestNonexistentDns()
            {
                var hostname = "nonexistent.example.com"; //Should not resolve
                Action testWrite = () => SwitchConfigHelper.DnsLookups.ResolveSingleA(hostname);
                Assert.Throws<SocketException>(testWrite);
            }

            [Fact]
            public void ResolveSingleATestExample()
            {
                var hostname = "www.example.com";
                //Don't check the actual resolved hostname
                var result = SwitchConfigHelper.DnsLookups.ResolveSingleA(hostname);
            }
        }

		public class ResolveMultipleATests
        {
            [Fact]
            public void ResolveMultipleATestLocalhost()
            {
                var hostname = "localhost";
                var expectedResult = new List<string> { "127.0.0.1" };
                var result = SwitchConfigHelper.DnsLookups.ResolveMultipleA(hostname);
                result.Should().Equal(expectedResult);
            }

            [Fact]
            public void ResolveMultipleATestGoogleDns()
            {
                var hostname = "dns.google"; //Should have A records for 8.8.8.8 and 8.8.4.4
                var expectedResult = new List<string> { "8.8.4.4", "8.8.8.8" };; //Should always be ordered numerically
                var result = SwitchConfigHelper.DnsLookups.ResolveMultipleA(hostname);
                result.Should().Equal(expectedResult);
            }

            [Fact]
            public void ResolveMultipleATestNonexistentDns()
            {
                var hostname = "nonexistent.example.com"; //Should not resolve
                Action testWrite = () => SwitchConfigHelper.DnsLookups.ResolveMultipleA(hostname);
                Assert.Throws<SocketException>(testWrite);
            }

            [Fact]
            public void ResolveMultipleATestExample()
            {
                var hostname = "www.example.com";
                //Don't check the actual resolved hostname
                var result = SwitchConfigHelper.DnsLookups.ResolveA(hostname);
            }
        }
    }
}