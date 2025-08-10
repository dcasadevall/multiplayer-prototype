using System;
using UnityEngine;
using UnityEngine.UI;

namespace Adapters.UI
{
    public class SplashScreen : MonoBehaviour
    {
        [SerializeField]
        private Button _playButton;

        [SerializeField]
        private GameLauncher _gameLauncher;

        [SerializeField] 
        private GameObject _loadingSpinner;

        [SerializeField] 
        private NetworkErrorModal _errorModal;
        
        private void Awake()
        {
            if (_gameLauncher == null)
            {
                Debug.LogError("RootServiceProvider is not assigned in the SplashScreen script.");
                return;
            }
            
            if (_playButton == null)
            {
                Debug.LogError("Play Button not assigned in the SplashScreen script.");
                return;
            }
            
            // Subscribe to events from the GameLauncher
            _gameLauncher.OnRootServicesConfigured += HandleServicesConfigured;
            _gameLauncher.OnGameStarted += HandleGameStarted;
            
            // Disable the play button until services are configured
            _playButton.enabled = false;
            
            // Show the splash screen
            gameObject.SetActive(true);
        }

        private void HandleServicesConfigured(IServiceProvider obj)
        {
            _playButton.onClick.AddListener(async () =>
            {
                try
                {
                    _loadingSpinner.SetActive(true);
                    _playButton.interactable = false;
                    await _gameLauncher.StartGameAsync();
                } 
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to start the game: {ex.Message}");
                    _errorModal.Show("Failed to connect to the server. Please try again later.");
                    _playButton.interactable = true;
                }
                finally
                {
                    _loadingSpinner.SetActive(false);
                }
            });
            
            _playButton.enabled = true;
            _playButton.interactable = true;
        }
        
        private void HandleGameStarted(IServiceProvider serviceProvider)
        {
            // Hide the splash screen
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            _playButton?.onClick.RemoveAllListeners();
            _gameLauncher.OnRootServicesConfigured -= HandleServicesConfigured;
            _gameLauncher.OnGameStarted -= HandleGameStarted;
        }
    }
}
