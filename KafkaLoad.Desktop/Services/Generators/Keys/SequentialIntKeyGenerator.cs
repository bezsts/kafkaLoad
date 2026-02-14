using System;
using System.Threading;

namespace KafkaLoad.Desktop.Services.Generators.Keys
{
    public class SequentialIntKeyGenerator : IDataGenerator
    {
        private int _counter = 0;

        public byte[] Next()
        {
            int val = Interlocked.Increment(ref _counter);
            return BitConverter.GetBytes(val);
        }
    }
}
