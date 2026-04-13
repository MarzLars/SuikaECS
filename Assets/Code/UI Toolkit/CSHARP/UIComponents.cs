using Unity.Entities;

namespace Suika.UI
{
    public struct Event : IComponentData { }

    public struct PlayClickEvent : IComponentData { }
    public struct RestartClickEvent : IComponentData { }

    public enum GameState
    {
        Init,
        Start,
        Playing,
        GameOver
    }

    public struct SuikaGameState : IComponentData
    {
        public GameState State;
    }

    public struct ScoreEvent : IComponentData
    {
        public int Amount;
    }

    public struct SuikaScore : IComponentData
    {
        public int Value;
    }

    public struct UIScreens : IComponentData
    {
        public UnityObjectRef<StartScreen> StartScreen;
        public UnityObjectRef<HUDScreen> HUDScreen;
        public UnityObjectRef<GameOverScreen> GameOverScreen;
    }
}
