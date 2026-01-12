using FluentResults;

namespace ahis.template.application.Shared.Errors
{
    public class EntityNotFoundError : Error
    {
        public EntityNotFoundError(string entityName, object key)
            : base($"Entity '{entityName}' with key '{key}' was not found.")
        {
            EntityName = entityName;
            Key = key;
        }
        public string EntityName { get; }
        public object Key { get; }
        public override string ToString()
        {
            return $"{base.ToString()} (Entity: {EntityName}, Key: {Key})";
        }
    }
}
