namespace SR2MP.Shared.Utils;

public interface IRecyclable
{
    bool IsRecycled { get; set; }

    void Recycle();
}