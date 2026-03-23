using KafkaLoad.Core.Services.Interfaces;
using System;
using System.Threading;

namespace KafkaLoad.Core.Services.Generators.Keys
{
    public class SequentialIntKeyGenerator : IDataGenerator
    {
        private int _counter = 0;

        public byte[] Next()
        {
            int val = Interlocked.Increment(ref _counter);
            byte[] bytes = BitConverter.GetBytes(val);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return bytes;
        }
    }
}
