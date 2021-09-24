using Orleans;
using System.Threading.Tasks;

namespace Interfaces
{
    public interface IMQMessagePrinter : IGrainWithStringKey
    {
        Task GroupA(string message);
        Task GroupB(string message);
    }
}