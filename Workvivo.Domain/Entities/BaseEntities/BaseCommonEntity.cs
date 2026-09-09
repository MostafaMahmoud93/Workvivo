namespace Workvivo.Domain.Entities.BaseEntities;
public class BaseCommonEntity<U> : BaseEntity
{
    public U Id { get; set; }
}
