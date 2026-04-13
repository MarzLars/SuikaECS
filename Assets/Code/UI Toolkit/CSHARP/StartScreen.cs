using Unity.Entities;
using UnityEngine.UIElements;

namespace Suika.UI
{
    // displays the play button
    public class StartScreen : UIScreen
    {
        Button m_PlayButton;

        public static StartScreen Instantiate(VisualElement parentElement) {
            var screen = CreateInstance<StartScreen>();
            screen.RootElement = parentElement;

            screen.m_PlayButton = screen.RootElement.Q<Button>("start__play-button");
            screen.m_PlayButton.clicked += screen.OnClickPlay;

            screen.RootElement.style.display = DisplayStyle.None;
            return screen;
        }

        public void OnClickPlay() {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null && world.IsCreated) {
                var entityManager = world.EntityManager;
                var entity = entityManager.CreateEntity();
                entityManager.AddComponentData(entity, new PlayClickEvent());
                entityManager.AddComponentData(entity, new Event());
            }
        }
    }
}
