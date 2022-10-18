namespace NuGetPe
{
    public interface IFolder : IPart
    {
        IPart? this[string name] { get; }
    }
}
