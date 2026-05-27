using UnityEngine.UIElements;

namespace Suika.UI
{
    public class FriendsScreen : UIScreen
    {
        Button m_CloseButton;

        public static FriendsScreen Instantiate(VisualElement parentElement) {
            var screen = CreateInstance<FriendsScreen>();
            screen.RootElement = parentElement;

            screen.m_CloseButton = screen.RootElement.Q<Button>("friends__close-button");
            screen.m_CloseButton.clicked += screen.Hide;
            screen.RootElement.style.display = DisplayStyle.None;
            return screen;
        }
    }
}
