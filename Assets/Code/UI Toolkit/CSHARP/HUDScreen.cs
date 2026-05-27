using System;
using Unity.Entities;
using UnityEngine.UIElements;

namespace Suika.UI
{
    // displays score and next fruit
    public class HUDScreen : UIScreen
    {
        VisualElement m_DropperBoundsLine;
        VisualElement m_DropperMarker;
        VisualElement m_NextFruitContainer;
        Label m_ScoreLabel;
        Button m_SettingsButton;
        
        Label m_OpponentLabel;

        public static HUDScreen Instantiate(VisualElement parentElement) {
            var screen = CreateInstance<HUDScreen>();
            screen.RootElement = parentElement;

            screen.m_ScoreLabel = screen.RootElement.Q<Label>("hud__score-label");
            screen.m_NextFruitContainer = screen.RootElement.Q<VisualElement>("hud__next-fruit-container");
            screen.m_DropperBoundsLine = screen.RootElement.Q<VisualElement>("hud__dropper-bounds-line");
            screen.m_DropperMarker = screen.RootElement.Q<VisualElement>("hud__dropper-marker");
            screen.m_SettingsButton = screen.RootElement.Q<Button>("hud__settings-button");
            
            screen.m_OpponentLabel = screen.RootElement.Q<Label>("hud__opponent-label");

            screen.m_SettingsButton.clicked += screen.OnClickSettings;
            
            screen.RootElement.style.display = DisplayStyle.None;
            return screen;
        }

        public void SetScore(int score) {
            m_ScoreLabel.text = $"Score: {score}";
        }

        public void SetOpponentScore(int score, string label = "Opponent to beat") {
            if (m_OpponentLabel != null) {
                m_OpponentLabel.text = $"{label}: {score}";
            }
        }

        public void SetDropperAimVisual(float markerCenterX, float markerCenterY, float boundsMinX, float boundsMaxX,
float boundsCenterY) {
            float left = MathF.Min(boundsMinX, boundsMaxX);
            float width = MathF.Max(0f, MathF.Abs(boundsMaxX - boundsMinX));

            m_DropperBoundsLine.style.left = left;
            m_DropperBoundsLine.style.top = boundsCenterY - 1f;
            m_DropperBoundsLine.style.width = width;

            var markerSize = 18f;
            m_DropperMarker.style.left = markerCenterX - markerSize * 0.5f;
            m_DropperMarker.style.top = markerCenterY - markerSize * 0.5f;
        }

        public void OnClickSettings() {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world is not { IsCreated: true }) return;

            var entityManager = world.EntityManager;
            var entity = entityManager.CreateEntity();
            entityManager.AddComponentData(entity, new OpenSettingsEvent { ReturnToState = GameState.Playing });
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

        // Additional methods for next fruit etc. could be added here
}
}