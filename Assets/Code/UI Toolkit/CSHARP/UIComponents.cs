using Unity.Entities;

namespace Suika.UI
{
    public struct Event : IComponentData
    { }

    public struct PlayClickEvent : IComponentData
    { }

    public struct RestartClickEvent : IComponentData
    { }

    // Created by ECS when a restart is requested; consumed by a MonoBehaviour that performs scene reload.
    public struct RestartRequest : IComponentData
    { }

    public struct GameOverEvent : IComponentData
    { }

    public struct OpenSettingsEvent : IComponentData
    {
        public GameState ReturnToState;
    }

    public struct OpenLeaderboardEvent : IComponentData
    { }

    public struct OpenFriendsEvent : IComponentData
    { }

    public struct ChallengeEvent : IComponentData
    {
        public int ScoreToBeat;
        public uint Seed;
    }

    public struct CloseSettingsEvent : IComponentData
{ }

    public struct ApplySettingsEvent : IComponentData
    {
        public uint Seed;
        public float AimMoveSpeed;
        public float AimMinX;
        public float AimMaxX;
        public float AccelerometerDeadZone;
        public float AccelerometerScale;
        public float AccelerometerSmoothing;
    }

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
        public UnityObjectRef<SettingsScreen> SettingsScreen;
        public UnityObjectRef<LeaderboardScreen> LeaderboardScreen;
        public UnityObjectRef<FriendsScreen> FriendsScreen;
    }
}