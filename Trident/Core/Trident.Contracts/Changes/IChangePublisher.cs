using System.Threading.Tasks;

namespace Trident.Contracts.Changes;

public interface IChangePublisher
{
    Task Publish(ChangeModel changeModel);
}
