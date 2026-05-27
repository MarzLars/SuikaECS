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

        public static StartScreen Instantiate(VisualElement parentElement) {
            var screen = CreateInstance<StartScreen>();
            screen.RootElement = parentElement;

            screen.m_PlayButton = screen.RootElement.Q<Button>("start__play-button");
            screen.m_SettingsButton = screen.RootElement.Q<Button>("start__settings-button");
            if (screen.m_PlayButton == null)
                throw new InvalidOperationException(
                    "Required UI element 'start__play-button' not found in StartScreen UXML.");
            if (screen.m_SettingsButton == null)
                throw new InvalidOperationException(
                    "Required UI element 'start__settings-button' not found in StartScreen UXML.");

            screen.m_PlayButton.clicked += screen.OnClickPlay;
            screen.m_SettingsButton.clicked += screen.OnClickSettings;

            screen.RootElement.style.display = DisplayStyle.None;
            return screen;
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
    }
}