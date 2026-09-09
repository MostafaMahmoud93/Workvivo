namespace Workvivo.Application.Bases;
public record CollectionResponse<T>(int Length, ICollection<T> Collection);
