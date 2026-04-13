using Unity.Entities;
using UnityEngine.UIElements;

namespace Suika.UI
{
    // displays final score and restart button
    public class GameOverScreen : UIScreen
    {
        Label m_FinalScoreLabel;
        Button m_RestartButton;

        public static GameOverScreen Instantiate(VisualElement parentElement) {
            var screen = CreateInstance<GameOverScreen>();
            screen.RootElement = parentElement;

            screen.m_FinalScoreLabel = screen.RootElement.Q<Label>("gameover__score-label");
            screen.m_RestartButton = screen.RootElement.Q<Button>("gameover__restart-button");

            screen.m_RestartButton.clicked += screen.OnClickRestart;

            screen.RootElement.style.display = DisplayStyle.None;
            return screen;
        }

        public void SetFinalScore(int score) {
            if (m_FinalScoreLabel != null) {
                m_FinalScoreLabel.text = $"Final Score: {score}";
            }
        }

        public void OnClickRestart() {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world is not { IsCreated: true }) return;
            var entityManager = world.EntityManager;
            var entity = entityManager.CreateEntity();
            entityManager.AddComponentData(entity, new RestartClickEvent());
            entityManager.AddComponentData(entity, new Event());
        }
    }
}
