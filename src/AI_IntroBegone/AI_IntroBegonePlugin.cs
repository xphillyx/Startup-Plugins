﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using AIProject;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace IntroBegone
{
    [BepInProcess("AI-Syoujyo")]
    [BepInProcess("AI-Shoujo")]
    public partial class IntroBegonePlugin : BaseUnityPlugin
    {
        protected const string SECTION_GENERAL = "General";

        protected const string DESCRIPTION_AUTOSTART = "Choose which mode to start automatically when launching the game.\n" +
                                                       "Hold esc or F1 during startup to cancel automatic behaviour or hold another shortcut to use that instead.";

        private static readonly Dictionary<string, Action<TitleScene>> _supportedArgs =
            new Dictionary<string, Action<TitleScene>>
            {
                {"-maker", scene => scene.OnCustomFemale()},
                {"-femalemaker", scene => scene.OnCustomFemale()},
                {"-malemaker", scene => scene.OnCustomMale()},
            };

        private ConfigEntry<AutoStartOption> AutoStart { get; set; }

        private bool _checkInput;
        private bool _cancelAuto;
        private TitleScene _titleScene;
        private Action<TitleScene> _autostartCommand;

        private void Awake()
        {
            // Check if any autostart parameters were passed
            var _ = Environment.GetCommandLineArgs().FirstOrDefault(x => _supportedArgs.TryGetValue(x.ToLowerInvariant(), out _autostartCommand));
            AutoStart = Config.Bind(SECTION_GENERAL, "Automatic start mode", AutoStartOption.Disabled, new ConfigDescription(DESCRIPTION_AUTOSTART));

            SceneManager.sceneLoaded += StartInput;
        }

        private void StartInput(Scene scene, LoadSceneMode mode)
        {
            StopAllCoroutines();

            _titleScene = FindObjectOfType<TitleScene>();

            if (_titleScene)
            {
                if (!_checkInput)
                {
                    _checkInput = true;
                    StartCoroutine(InputCheck());
                }
            }
            else
            {
                _checkInput = false;
            }
        }

        private IEnumerator InputCheck()
        {
            while (_checkInput)
            {
                if (!_cancelAuto && AutoStart.Value != AutoStartOption.Disabled && (Input.GetKey(KeyCode.Escape) || Input.GetKey(KeyCode.F1)))
                {
                    Logger.Log(BepInEx.Logging.LogLevel.Message, "Automatic start cancelled");
                    _cancelAuto = true;
                }

                if (!Manager.Scene.Instance.IsNowLoadingFade)
                {
                    if (_autostartCommand != null)
                    {
                        _autostartCommand(_titleScene);
                        _autostartCommand = null;
                        _checkInput = false;
                    }
                    else if (!_cancelAuto && AutoStart.Value != AutoStartOption.Disabled)
                    {
                        switch (AutoStart.Value)
                        {
                            case AutoStartOption.FemaleMaker:
                                StartMode(_titleScene.OnCustomFemale, "Automatically starting female maker");
                                break;

                            case AutoStartOption.MaleMaker:
                                StartMode(_titleScene.OnCustomMale, "Automatically starting male maker");
                                break;
                        }
                    }

                    _cancelAuto = true;
                }

                yield return null;
            }
        }

        private void StartMode(UnityAction action, string msg)
        {
            if (!FindObjectOfType<ConfigScene.ConfigWindow>())
            {
                Logger.LogMessage(msg);
                _checkInput = false;
                action();
            }
        }

        private enum AutoStartOption
        {
            Disabled,
            [Description("Female maker")]
            FemaleMaker,
            [Description("Male maker")]
            MaleMaker
        }
    }

}