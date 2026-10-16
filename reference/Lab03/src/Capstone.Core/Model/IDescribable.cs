namespace Capstone.Core.Model;

/// <summary>
/// Anything that can describe itself in one line: a Pokémon, a trainer. Two very different types,
/// one shared capability. That's what an interface is for. Our own interface: the contracts don't count.
/// </summary>
public interface IDescribable
{
    string Describe();
}
