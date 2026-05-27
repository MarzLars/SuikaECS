using System;
using Unity.Entities;
using UnityEngine.UIElements;

namespace Suika.UI
{
    public class SettingsScreen : UIScreen
    {
        FloatField m_AccelerometerDeadZoneField;
        FloatField m_AccelerometerScaleField;
        FloatField m_AccelerometerSmoothingField;
        FloatField m_AimMaxXField;
        FloatField m_AimMinXField;

        FloatField m_AimMoveSpeedField;
        Button m_ApplyButton;
        Button m_BackButton;
        GameState m_ReturnToState;
        IntegerField m_SeedField;

        public static SettingsScreen Instantiate(VisualElement parentElement) {
            var screen = CreateInstance<SettingsScreen>();
            screen.RootElement = parentElement;

            screen.m_SeedField = screen.RootElement.Q<IntegerField>("settings__seed");
            screen.m_AimMoveSpeedField = screen.RootElement.Q<FloatField>("settings__aim-move-speed");
            screen.m_AimMinXField = screen.RootElement.Q<FloatField>("settings__aim-min-x");
            screen.m_AimMaxXField = screen.RootElement.Q<FloatField>("settings__aim-max-x");
            screen.m_AccelerometerDeadZoneField = screen.RootElement.Q<FloatField>("settings__accelerometer-deadzone");
            screen.m_AccelerometerScaleField = screen.RootElement.Q<FloatField>("settings__accelerometer-scale");
            screen.m_AccelerometerSmoothingField =
                screen.RootElement.Q<FloatField>("settings__accelerometer-smoothing");
            screen.m_ApplyButton = screen.RootElement.Q<Button>("settings__apply-button");
            screen.m_BackButton = screen.RootElement.Q<Button>("settings__back-button");

            if (screen.m_SeedField == null)
                throw new InvalidOperationException(
                    "Required UI element 'settings__seed' not found in SettingsScreen UXML.");
            if (screen.m_AimMoveSpeedField == null)
                throw new InvalidOperationException(
                    "Required UI element 'settings__aim-move-speed' not found in SettingsScreen UXML.");
            if (screen.m_AimMinXField == null)
                throw new InvalidOperationException(
                    "Required UI element 'settings__aim-min-x' not found in SettingsScreen UXML.");
            if (screen.m_AimMaxXField == null)
                throw new InvalidOperationException(
                    "Required UI element 'settings__aim-max-x' not found in SettingsScreen UXML.");
            if (screen.m_AccelerometerDeadZoneField == null)
                throw new InvalidOperationException(
                    "Required UI element 'settings__accelerometer-deadzone' not found in SettingsScreen UXML.");
            if (screen.m_AccelerometerScaleField == null)
                throw new InvalidOperationException(
                    "Required UI element 'settings__accelerometer-scale' not found in SettingsScreen UXML.");
            if (screen.m_AccelerometerSmoothingField == null)
                throw new InvalidOperationException(
                    "Required UI element 'settings__accelerometer-smoothing' not found in SettingsScreen UXML.");
            if (screen.m_ApplyButton == null)
                throw new InvalidOperationException(
                    "Required UI element 'settings__apply-button' not found in SettingsScreen UXML.");
            if (screen.m_BackButton == null)
                throw new InvalidOperationException(
                    "Required UI element 'settings__back-button' not found in SettingsScreen UXML.");

            screen.m_ApplyButton.clicked += screen.OnClickApply;
            screen.m_BackButton.clicked += screen.OnClickBack;

            screen.RootElement.style.display = DisplayStyle.None;
            return screen;
        }

        public void SetValues(uint seed, float aimMoveSpeed, float aimMinX, float aimMaxX, float accelerometerDeadZone,
            float accelerometerScale, float accelerometerSmoothing) {
            m_SeedField.value = unchecked((int)seed);
            m_AimMoveSpeedField.value = aimMoveSpeed;
            m_AimMinXField.value = aimMinX;
            m_AimMaxXField.value = aimMaxX;
            m_AccelerometerDeadZoneField.value = accelerometerDeadZone;
            m_AccelerometerScaleField.value = accelerometerScale;
            m_AccelerometerSmoothingField.value = accelerometerSmoothing;
        }

        public void SetReturnToState(GameState returnToState) {
            m_ReturnToState = returnToState;
        }

        public GameState GetReturnToState() {
            return m_ReturnToState;
        }

        public ApplySettingsEvent GetSettingsSnapshot() {
            return new ApplySettingsEvent {
                Seed = unchecked((uint)m_SeedField.value),
                AimMoveSpeed = m_AimMoveSpeedField.value,
                AimMinX = m_AimMinXField.value,
                AimMaxX = m_AimMaxXField.value,
                AccelerometerDeadZone = m_AccelerometerDeadZoneField.value,
                AccelerometerScale = m_AccelerometerScaleField.value,
                AccelerometerSmoothing = m_AccelerometerSmoothingField.value
            };
        }

        public void OnClickApply() {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world is not { IsCreated: true }) return;

            var entityManager = world.EntityManager;
            var entity = entityManager.CreateEntity();
            entityManager.AddComponentData(entity, GetSettingsSnapshot());
            entityManager.AddComponentData(entity, new Event());
        }

        public void OnClickBack() {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world is not { IsCreated: true }) return;

            var entityManager = world.EntityManager;
            var entity = entityManager.CreateEntity();
            entityManager.AddComponentData(entity, new CloseSettingsEvent());
            entityManager.AddComponentData(entity, new Event());
        }
    }
}