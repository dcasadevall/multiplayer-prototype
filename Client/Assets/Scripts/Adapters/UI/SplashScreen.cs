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
        private Button _localPlayButton;

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
            
            if (_loadingSpinner == null)
            {
                Debug.LogError("Loading spinner not assigned in the SplashScreen script.");
                return;
            }
            
            if (_errorModal == null)
            {
                Debug.LogError("Error modal not assigned in the SplashScreen script.");
                return;
            }
            
            if (_localPlayButton == null)
            {
                Debug.LogError("Local Play Button not assigned in the SplashScreen script.");
                return;
            }
            
            // Subscribe to events from the GameLauncher
            _gameLauncher.OnRootServicesConfigured += HandleServicesConfigured;
            _gameLauncher.OnGameStarted += HandleGameStarted;
            
            // Disable the play buttons until services are configured
            _playButton.enabled = false;
            _localPlayButton.enabled = false;
            
            // Show the splash screen
            gameObject.SetActive(true);
        }

        private void HandleServicesConfigured(IServiceProvider obj)
        {
            _playButton.onClick.AddListener(() => HandlePlayClicked(remotePlay: true));
            _localPlayButton.onClick.AddListener(() => HandlePlayClicked(remotePlay: false));

            _localPlayButton.enabled = true;
            _localPlayButton.interactable = true;
            _playButton.enabled = true;
            _playButton.interactable = true;
        }

        private async void HandlePlayClicked(bool remotePlay)
        {
            try
            {
                _loadingSpinner.SetActive(true);
                _playButton.interactable = false;
                _localPlayButton.interactable = false;
                await _gameLauncher.StartGameAsync(remotePlay);
            } 
            catch (Exception ex)
            {
                Debug.LogError($"Failed to start the game: {ex.Message}");
                _errorModal.Show("Failed to connect to the server. Please try again later.");
                _playButton.interactable = true;
                _localPlayButton.interactable = true;
            }
            finally
            {
                _loadingSpinner.SetActive(false);
            } 
        }
        
        private void HandleGameStarted(IServiceProvider serviceProvider)
        {
            // Hide the splash screen
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            _playButton?.onClick.RemoveAllListeners();
            _localPlayButton.onClick.RemoveAllListeners();
            _gameLauncher.OnRootServicesConfigured -= HandleServicesConfigured;
            _gameLauncher.OnGameStarted -= HandleGameStarted;
        }
    }
}
