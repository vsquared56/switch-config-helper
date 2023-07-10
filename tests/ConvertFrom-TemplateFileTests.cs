using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SwitchConfigHelper.Tests
{
	public class ConvertFromTemplateFileTests
	{
		public class ConvertFromTemplateFile
		{
			public class DnsLookupsTests
			{
				[Fact]
				public void ResolveATestLocalhost()
				{
					// Arrange.
					var path = "simple_dns_test.txt";
					var cmdlet = new ConvertFromTemplateFileCommand()
					{
						TemplatePath = path,
					};
					var expectedResult = "127.0.0.1";

					// Act.
					var results = cmdlet.Invoke().OfType<string>().ToList();

					// Assert.
					Assert.Equal(expectedResult, results.First());
					Assert.True(results.Count == 1);
				}
			}
		}
	}
}
