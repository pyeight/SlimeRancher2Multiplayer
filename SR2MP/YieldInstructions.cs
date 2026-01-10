using System.Collections;
using Il2CppMonomiPark.SlimeRancher.SceneManagement;

namespace SR2MP;

public abstract class MPYieldInstruction : IEnumerator
{
    public bool MoveNext()
    {
        return ShouldWait;
    }

    public void Reset() { }
    
    public object? Current => null;
    
    public abstract bool ShouldWait { get; }
}

public class WaitForSceneGroupLoad : MPYieldInstruction
{
    public WaitForSceneGroupLoad(bool state = true) { this.state = state; }

    private bool state;
    private SceneLoader sceneLoader = SystemContext.Instance.SceneLoader;
    
    public override bool ShouldWait => sceneLoader.IsSceneLoadInProgress == state;
}
