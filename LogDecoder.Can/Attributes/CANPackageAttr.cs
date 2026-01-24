namespace LogDecoder.CAN.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class CanPackageAttr : Attribute
{
    public int Id { get; }
    public string Name { get; }

    public CanPackageAttr(int id, string name)
    {
        Id = id;
        Name = name;
    }
}
