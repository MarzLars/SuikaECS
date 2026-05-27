using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Services.Leaderboards.Models;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suika.UI
{
    public class LeaderboardScreen : UIScreen
    {
        ScrollView m_LeaderboardList;
        Button m_CloseButton;
        VisualTreeAsset m_EntryTemplate;

        public static LeaderboardScreen Instantiate(VisualElement parentElement, VisualTreeAsset entryTemplate) {
            var screen = CreateInstance<LeaderboardScreen>();
            screen.RootElement = parentElement;
            screen.m_EntryTemplate = entryTemplate;

            screen.m_LeaderboardList = screen.RootElement.Q<ScrollView>("leaderboard__list");
            screen.m_CloseButton = screen.RootElement.Q<Button>("leaderboard__close-button");

            screen.m_CloseButton.clicked += screen.Hide;
            screen.RootElement.style.display = DisplayStyle.None;
            return screen;
        }

        [Serializable]
        private class EntryMetadata {
            public uint seed;
        }

        public void Populate(LeaderboardScoresPage scores) {
            m_LeaderboardList.Clear();
            foreach (var entry in scores.Results) {
                var entryElement = m_EntryTemplate.Instantiate();
                var nameLabel = entryElement.Q<Label>("entry__name");
                var scoreLabel = entryElement.Q<Label>("entry__score");
                var challengeButton = entryElement.Q<Button>("entry__challenge-button");

                nameLabel.text = entry.PlayerName ?? "Anonymous";
                scoreLabel.text = ((int)entry.Score).ToString();

                int scoreToBeat = (int)entry.Score;
                uint seed = 0;
                if (!string.IsNullOrEmpty(entry.Metadata)) {
                    try {
                        var meta = JsonUtility.FromJson<EntryMetadata>(entry.Metadata);
                        seed = meta.seed;
                    } catch (Exception) { /* ignore malformed metadata */ }
                }

                challengeButton.clicked += () => OnChallenge(scoreToBeat, seed);

                m_LeaderboardList.Add(entryElement);
            }
        }

        void OnChallenge(int score, uint seed) {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world is not { IsCreated: true }) return;

            var entityManager = world.EntityManager;
            var entity = entityManager.CreateEntity();
            entityManager.AddComponentData(entity, new ChallengeEvent { ScoreToBeat = score, Seed = seed });
            entityManager.AddComponentData(entity, new Event());
            
            Hide();
        }
}
}
