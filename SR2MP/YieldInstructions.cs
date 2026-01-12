using System.Collections;
using Il2CppMonomiPark.SlimeRancher.SceneManagement;

namespace SR2MP;

public abstract class MpYieldInstruction : IEnumerator
{
    public bool MoveNext() => ShouldWait;

    public void Reset() { }

    public object? Current => null;

    protected abstract bool ShouldWait { get; }
}

public sealed class WaitForSceneGroupLoad : MpYieldInstruction
{
    private bool _state;
    private SceneLoader sceneLoader = SystemContext.Instance.SceneLoader;

    public WaitForSceneGroupLoad(bool state = true) => _state = state;

    protected override bool ShouldWait => sceneLoader.IsSceneLoadInProgress == _state;
}