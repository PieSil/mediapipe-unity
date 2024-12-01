using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class GameLogic : MonoBehaviour {

    [SerializeField] private AimCalibrator _calibrator;
    [SerializeField] private UIManager _uiManager;
    [SerializeField] private LevelManager _levelManager;
    [SerializeField] private HandInputManager _handInputManager;
    private Stack<GameState> _stateStack = new Stack<GameState>();
    private int _highScore = 0;

    private void Start() {
        StartCalibration();
        _levelManager.NewScoreEvent += OnScoredPoint;
        _levelManager.GameOverEvent += OnGameOver;
    }

    private void Update() {
        if (CurState() == GameState.PLAYING && (Input.GetKeyDown(KeyCode.Escape) || _handInputManager.GetHandInputDown(HandInputType.HAND_OPEN))) {
            PauseGame();
        } else if (CurState() == GameState.PAUSED && Input.GetKeyDown(KeyCode.Escape)) {
            ResumeGame();
        }
    }

    public void PauseGame() {
        if (CurState() != GameState.PAUSED) {
            PushState(GameState.PAUSED);
            OnNewState();
        }
    }

    public void ResumeGame() {
        GameState expectPaused = PopState();
        OnNewState();

        if (expectPaused != GameState.PAUSED) {
            throw new System.Exception($"Expected game state to be PAUSED, got {expectPaused}");
        }
    }

    public void StartGame() {

        if (CurState() != GameState.MAIN_MENU) {
            throw new System.Exception($"Expected game state to be MAIN_MENU, got {CurState()}");
        }

        PushState(GameState.PLAYING);
        OnNewState();
        _levelManager.StartLevel();
    }

    public void GoToMainMenu() {

        _levelManager.Clear();

        while (CurState() != GameState.MAIN_MENU || _stateStack.Count == 0) {
            PopState();
        }

        if (_stateStack.Count == 0) {
            PushState(GameState.MAIN_MENU);
        }

        OnNewState();
    }

    public void GameOver() {

    } 

    public void StartCalibration() {
        PushState(GameState.CALIBRATING);
        OnNewState();
        _calibrator.CalibrationDone += OnCalibrationDone;
        _calibrator.ResetCalibration();
    }

    public void GoBackOneState() {
        PopState();
        OnNewState();
    }

    public void Quit() {
        Application.Quit();
    }

    private void OnCalibrationDone(EventArgs args) {
        _calibrator.CalibrationDone -= OnCalibrationDone;
        PopState();
        if (CurState() == GameState.NONE) {
            // first calibration done, push main menu
            PushState(GameState.MAIN_MENU);
        }

        OnNewState();

    }

    private GameState CurState() {
        if (_stateStack.Count > 0) {
            return _stateStack.Peek();
        } else {
            return GameState.NONE;
        }
    }

    private void PushState(GameState state) {
        _stateStack.Push(state);
    }

    private GameState PopState() {
        if (_stateStack.Count > 0) { 
            return _stateStack.Pop();
        } else {
            return GameState.NONE;
        }
    }

    private void OnNewState() {
        var newState = CurState();
        var systemState = SystemState.GetInstance();

        switch (newState) {
            case GameState.PAUSED:
                systemState.Pause();
                _handInputManager.EnablePointer();
                systemState.SetMouseEnabled(true);
                _uiManager.DrawPauseMenu();
                break;
            case GameState.PLAYING:
                systemState.Resume();
                _handInputManager.EnablePointer();
                systemState.SetMouseEnabled(false);
                _uiManager.DrawGameUI();
                break;
            case GameState.MAIN_MENU:
                systemState.Pause();
                _handInputManager.EnablePointer();
                systemState.SetMouseEnabled(true);
                _uiManager.DrawMainMenu(_highScore);
                break;
            case GameState.CALIBRATING:
                systemState.Pause();
                _handInputManager.DisablePointer();
                systemState.SetMouseEnabled(false);
                _uiManager.DisableAllUIElements();
                break;
            case GameState.CONTROLS:
                systemState.Pause();
                _handInputManager.EnablePointer();
                systemState.SetMouseEnabled(true);
                _uiManager.DrawControls();
                break;
            case GameState.GAME_OVER:
                systemState.Pause();
                _handInputManager.EnablePointer();
                systemState.SetMouseEnabled(true);
                _uiManager.DrawGameOver();
                break;
            case GameState.INTRO:
                systemState.Pause();
                _handInputManager.DisablePointer();
                systemState.SetMouseEnabled(true);
                // _uiManager.DrawControls();
                break;
            default:
                break;
        }
    }

    private GameState ReplaceState(GameState state) {
        GameState ret = PopState();
        PushState(state);

        return ret;
    }

    private void ClearStates() {
        _stateStack.Clear();
    }

    private void OnScoredPoint(int score) {
        if (score > _highScore) {
            _highScore = score;
        }
    }

    private void OnGameOver() {
        _levelManager.Clear();

        while (_stateStack.Count > 1) {
            _stateStack.Pop();
        }

        var expectMainMenu = CurState();
        
        if (expectMainMenu != GameState.MAIN_MENU) {
            Debug.LogError($"Expected MAIN_MENU as stack base when transitioning to GAME_OVER, got {expectMainMenu}");
        }

        PushState(GameState.GAME_OVER);
        OnNewState();
    }
}

public enum GameState {
    NONE, 
    MAIN_MENU,
    PLAYING,
    PAUSED,
    CALIBRATING,
    CONTROLS,
    INTRO,
    GAME_OVER
}