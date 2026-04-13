using Unity.Entities;
using UnityEngine.UIElements;

namespace Suika.UI
{
    // displays score and next fruit
    public class HUDScreen : UIScreen
    {
        Label m_ScoreLabel;
        VisualElement m_NextFruitContainer;

        public static HUDScreen Instantiate(VisualElement parentElement) {
            var screen = CreateInstance<HUDScreen>();
            screen.RootElement = parentElement;

            screen.m_ScoreLabel = screen.RootElement.Q<Label>("hud__score-label");
            screen.m_NextFruitContainer = screen.RootElement.Q<VisualElement>("hud__next-fruit-container");

            screen.RootElement.style.display = DisplayStyle.None;
            return screen;
        }

        public void SetScore(int score) {
            if (m_ScoreLabel != null) {
                m_ScoreLabel.text = $"Score: {score}";
            }
        }
        
        // Additional methods for next fruit etc. could be added here
    }
}
