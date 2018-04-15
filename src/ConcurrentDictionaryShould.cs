//-----------------------------------------------------------------------
// <copyright file="ADFilterSampleTest.cs" company="Mr Matrix Mariusz Krzanowski">
//     (c) 2018 Mr Matrix Mariusz Krzanowski 
// </copyright>
// <author>Mariusz Krzanowski</author>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//
//-----------------------------------------------------------------------

namespace MrMatrix.Net.ConcurrentDictionaryRaceCondition
{
    using FluentAssertions;
    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using Xunit;

    public class ConcurrentDictionaryShould
    {
        [Fact]
        public void CallTwiceResolverWhileParallelExecution()
        {
            //// This counter is incremented while value resolving delegate is called.
            int executionCounter = 0;
            var dictionaryUnderTest = new ConcurrentDictionary<int, int>();

            var rootSynchronizationPoint = new SemaphoreSlim(0);
            var addSynchronizationPoint = new SemaphoreSlim(0);

            Action newKeyResolve = () => dictionaryUnderTest.GetOrAdd(1, (key) =>
            {
                //// Here value resolver informs root thread that delegate execution is started
                rootSynchronizationPoint.Release();
                //// Now we are waiting when root thread realize that both resolvers are started
                addSynchronizationPoint.Wait();
                int counter = Interlocked.Increment(ref executionCounter);
                return counter * 100 + key;
            });

            //// Executing resolvers in different thread. 
            //// NOTE!!! Execution MUST be multi threaded. 
            //// Thats why class Thread instead of class Task is used. 
            Thread t1 = new Thread(new ThreadStart(newKeyResolve));
            Thread t2 = new Thread(new ThreadStart(newKeyResolve));
            t1.Start();
            t2.Start();

            //// Waiting for both resolvers to be started
            rootSynchronizationPoint.Wait();
            rootSynchronizationPoint.Wait();
            //// Waiting for both resolvers to be started
            addSynchronizationPoint.Release(2);
            t1.Join();
            t2.Join();
            
            //// Waiting for both resolvers to be started
            executionCounter.Should().Be(2);
        }
    }
}
