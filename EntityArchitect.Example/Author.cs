using EntityArchitect.CRUD.Attributes;
using EntityArchitect.Entities.Attributes;
using EntityArchitect.Entities.Entities;

namespace EntityArchitect.Example;

[HasLightList]
public class Author : Entity
{
    [LightListProperty]
    public string Name { get; private set; }

    [RelationManyToOne<Book>(nameof(Book.Author)), IgnorePostRequest, IgnorePutRequest,IncludeInGet]
    public List<Book> Books { get; private set; }
}