﻿using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions_test.asyncrx
{
    [TestFixture]
    public class UnitTest1
    {
        [Test]
        public async Task TestMethod1()
        {
            await Task.CompletedTask;
        }
    }
}
