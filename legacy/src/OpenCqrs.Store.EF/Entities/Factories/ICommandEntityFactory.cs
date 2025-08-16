using OpenCqrs.Domain;

namespace OpenCqrs.Store.EF.Entities.Factories
{
    public interface ICommandEntityFactory
    {
        CommandEntity CreateCommand(IDomainCommand command);
    }
}
