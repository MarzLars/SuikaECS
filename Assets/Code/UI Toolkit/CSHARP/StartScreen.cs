using System;
using Unity.Entities;
using UnityEngine.UIElements;

namespace Suika.UI
{
    // displays the play button
    public class StartScreen : UIScreen
    {
        Button m_PlayButton;
        Button m_SettingsButton;
        Button m_LeaderboardButton;
        Button m_FriendsButton;

        public static StartScreen Instantiate(VisualElement parentElement) {
            var screen = CreateInstance<StartScreen>();
            screen.RootElement = parentElement;

            screen.m_PlayButton = screen.RootElement.Q<Button>("start__play-button");
            screen.m_SettingsButton = screen.RootElement.Q<Button>("start__settings-button");
            screen.m_LeaderboardButton = screen.RootElement.Q<Button>("start__leaderboard-button");
            screen.m_FriendsButton = screen.RootElement.Q<Button>("start__friends-button");

            if (screen.m_PlayButton == null)
                throw new InvalidOperationException(
                    "Required UI element 'start__play-button' not found in StartScreen UXML.");
            if (screen.m_SettingsButton == null)
                throw new InvalidOperationException(
                    "Required UI element 'start__settings-button' not found in StartScreen UXML.");

            screen.m_PlayButton.clicked += screen.OnClickPlay;
            screen.m_SettingsButton.clicked += screen.OnClickSettings;
            
            if (screen.m_LeaderboardButton != null)
                screen.m_LeaderboardButton.clicked += screen.OnClickLeaderboard;
            if (screen.m_FriendsButton != null)
                screen.m_FriendsButton.clicked += screen.OnClickFriends;

            screen.RootElement.style.display = DisplayStyle.None;
            return screen;
        }

        public void SetSocialButtonsEnabled(bool enabled) {
            m_LeaderboardButton?.SetEnabled(enabled);
            m_FriendsButton?.SetEnabled(enabled);
        }

        public void OnClickPlay() {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world is not { IsCreated: true }) return;
            var entityManager = world.EntityManager;
            var entity = entityManager.CreateEntity();
            entityManager.AddComponentData(entity, new PlayClickEvent());
            entityManager.AddComponentData(entity, new Event());
        }

        public void OnClickSettings() {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world is not { IsCreated: true }) return;

            var entityManager = world.EntityManager;
            var entity = entityManager.CreateEntity();
            entityManager.AddComponentData(entity, new OpenSettingsEvent { ReturnToState = GameState.Start });
            entityManager.AddComponentData(entity, new Event());
        }

        public void OnClickLeaderboard() {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world is not { IsCreated: true }) return;

            var entityManager = world.EntityManager;
            var entity = entityManager.CreateEntity();
            entityManager.AddComponentData(entity, new OpenLeaderboardEvent());
            entityManager.AddComponentData(entity, new Event());
        }

        public void OnClickFriends() {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world is not { IsCreated: true }) return;

            var entityManager = world.EntityManager;
            var entity = entityManager.CreateEntity();
            entityManager.AddComponentData(entity, new OpenFriendsEvent());
            entityManager.AddComponentData(entity, new Event());
        }
    }
}