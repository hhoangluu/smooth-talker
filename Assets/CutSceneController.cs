using UnityEngine;
using UnityEngine.Playables;
using ConversionSystem.Core;

public class CutSceneController : MonoBehaviour
{
    public PlayableDirector Timeline;
    public float DelayAfterTimeline = 2f;
    public string GamePlaySceneName = "GamePlay";

    private bool _timelineEnded;

    private void OnEnable()
    {
        Timeline.stopped += OnTimelineStopped;
    }

    private void OnDisable()
    {
        Timeline.stopped -= OnTimelineStopped;
    }

    private void OnTimelineStopped(PlayableDirector director)
    {
        if (_timelineEnded) return;
        _timelineEnded = true;
        Invoke(nameof(LoadGamePlay), DelayAfterTimeline);
    }

    private void LoadGamePlay()
    {
        GameManager.Instance.TransitionToScene(GamePlaySceneName);
    }
}
