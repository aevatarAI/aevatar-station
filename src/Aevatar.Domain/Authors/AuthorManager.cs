using System;
using System.Threading.Tasks;
using Aevatar.Domain.Grains.Authors;
using Orleans;
using Volo.Abp;
using Volo.Abp.Domain.Services;

namespace Aevatar.Authors;

public class AuthorManager : DomainService
{
    private readonly IAuthorRepository _authorRepository;
    private readonly IClusterClient _clusterClient;

    public AuthorManager(IAuthorRepository authorRepository, IClusterClient clusterClient)
    {
        _authorRepository = authorRepository;
        _clusterClient = clusterClient;
    }

    public async Task<Author> CreateAsync(
        string name,
        DateTime birthDate,
        string? shortBio = null)
    {
        Check.NotNullOrWhiteSpace(name, nameof(name));

        var existingAuthor = await _authorRepository.FindByNameAsync(name);
        if (existingAuthor != null)
        {
            throw new AuthorAlreadyExistsException(name);
        }

        return new Author(
            GuidGenerator.Create(),
            name,
            birthDate,
            shortBio
        );
    }

    public async Task ChangeNameAsync(
        Author author,
        string newName)
    {
        Check.NotNull(author, nameof(author));
        Check.NotNullOrWhiteSpace(newName, nameof(newName));

        var nameValidatorGrain = _clusterClient.GetGrain<INameValidator>(author.Id);
        var isValid = await nameValidatorGrain.IsValid(newName);
        
        if (!isValid)
        {
            throw new ArgumentException($"Name '{newName}' is not valid! It must contain at least a first name and last name.", nameof(newName));
        }

        var existingAuthor = await _authorRepository.FindByNameAsync(newName);
        if (existingAuthor != null && existingAuthor.Id != author.Id)
        {
            throw new AuthorAlreadyExistsException(newName);
        }

        author.ChangeName(newName);
    }
}
