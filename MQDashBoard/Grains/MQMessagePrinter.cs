using Interfaces;
using Orleans;
using System;
using System.Threading.Tasks;

namespace Grains
{
    public class MQMessagePrinter : Grain, IMQMessagePrinter
    {
        public async Task GroupA(string message)
        {
            Console.WriteLine(message);
            await Task.CompletedTask;
        }

        public async Task GroupB(string message)
        {
            Console.WriteLine(message);
            await Task.CompletedTask;
        }
    }
}
