﻿using IllusionPlugin;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace EnhancedTwitchChat
{
    public class OldConfigOptions
    {
        public string TwitchChannel = "";
    }

    public class Config
    {
        public string FilePath { get; }

        public string TwitchChannelName = "";
        public string TwitchUsername = "";
        public string TwitchOAuthToken = "";
        public string FontName = "Segoe UI";
        //public int BombBitValue;
        //public int TwitchBitBalance;

        public float ChatScale = 1.1f;
        public float ChatWidth = 160;
        public float MessageSpacing = 2.0f;
        public int MaxChatLines = 30;
        public float MinimumRating = 50.0f;

        public float PositionX = 2.0244143f;
        public float PositionY = 0.373768f;
        public float PositionZ = 0.08235432f;

        public float RotationX = 2.026023f;
        public float RotationY = 97.58616f;
        public float RotationZ = 1.190764f;

        public float TextColorR = 1;
        public float TextColorG = 1;
        public float TextColorB = 1;
        public float TextColorA = 1;

        public float BackgroundColorR = 0;
        public float BackgroundColorG = 0;
        public float BackgroundColorB = 0;
        public float BackgroundColorA = 0.6f;
        public float BackgroundPadding = 4;

        public bool LockChatPosition = false;
        public bool ReverseChatOrder = false;
        public bool AnimatedEmotes = true;
        public bool DrawShadows = false;
        public bool SongRequestBot = false;
        public bool SkipConfirmation = true;

        public string RequestCommandAliases = "request,bsr,add";
        public int RequestLimit = 5;
        public int RequestCooldownMinutes = 5;
        public string SongBlacklist = "";

        public event Action<Config> ConfigChangedEvent;

        private readonly FileSystemWatcher _configWatcher;
        private bool _saving;

        public static Config Instance = null;

        public List<string> Blacklist
        {
            get
            {
                List<string> blacklist = new List<string>();
                if (SongBlacklist != String.Empty)
                {
                    foreach (string s in SongBlacklist.Split(','))
                        blacklist.Add(s);
                }
                return blacklist;
            }
            set
            {
                SongBlacklist = string.Join(",", value.Distinct());
                Save();
            }
        }

        public Color TextColor
        {
            get
            {
                return new Color(TextColorR, TextColorG, TextColorB, TextColorA);
            }
            set
            {
                TextColorR = value.r;
                TextColorG = value.g;
                TextColorB = value.b;
                TextColorA = value.a;
            }
        }

        public Color BackgroundColor
        {
            get
            {
                return new Color(BackgroundColorR, BackgroundColorG, BackgroundColorB, BackgroundColorA);
            }
            set
            {
                BackgroundColorR = value.r;
                BackgroundColorG = value.g;
                BackgroundColorB = value.b;
                BackgroundColorA = value.a;
            }
        }

        public Vector3 ChatPosition
        {
            get
            {
                return new Vector3(PositionX, PositionY, PositionZ);
            }
            set
            {
                PositionX = value.x;
                PositionY = value.y;
                PositionZ = value.z;
            }
        }

        public Vector3 ChatRotation
        {
            get { return new Vector3(RotationX, RotationY, RotationZ); }
            set
            {
                RotationX = value.x;
                RotationY = value.y;
                RotationZ = value.z;
            }
        }

        public Config(string filePath)
        {
            Instance = this;
            FilePath = filePath;

            if(!Directory.Exists(Path.GetDirectoryName(FilePath)))
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));

            if (File.Exists(FilePath))
            {
                Load();
                
                if (File.ReadAllText(FilePath).Contains("TwitchChannel="))
                {
                    var oldConfig = new OldConfigOptions();
                    ConfigSerializer.LoadConfig(oldConfig, FilePath);

                    TwitchChannelName = oldConfig.TwitchChannel;
                }
            }
            CorrectConfigSettings();
            Save();

            _configWatcher = new FileSystemWatcher(Path.Combine(Environment.CurrentDirectory, "UserData"))
            {
                NotifyFilter = NotifyFilters.LastWrite,
                Filter = "EnhancedTwitchChat.ini",
                EnableRaisingEvents = true
            };
            _configWatcher.Changed += ConfigWatcherOnChanged;
        }

        ~Config()
        {
            _configWatcher.Changed -= ConfigWatcherOnChanged;
        }

        public void Load()
        {
            ConfigSerializer.LoadConfig(this, FilePath);

            CorrectConfigSettings();
        }

        public void Save(bool callback = false)
        {
            if (!callback)
                _saving = true;
            ConfigSerializer.SaveConfig(this, FilePath);
        }

        private void ImportAsyncTwitchConfig()
        {
            try
            {
                string asyncTwitchConfig = Path.Combine(Environment.CurrentDirectory, "UserData", "AsyncTwitchConfig.json");
                if (File.Exists(asyncTwitchConfig))
                {
                    JSONNode node = JSON.Parse(File.ReadAllText(asyncTwitchConfig));
                    if (!node.IsNull)
                    {
                        if (node["Username"].IsString && TwitchUsername == String.Empty)
                            TwitchUsername = node["Username"].Value;
                        if (node["ChannelName"].IsString && TwitchChannelName == String.Empty)
                            TwitchChannelName = node["ChannelName"].Value;
                        if (node["OauthKey"].IsString && TwitchOAuthToken == String.Empty)
                            TwitchOAuthToken = node["OauthKey"].Value;
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.Log($"Error when trying to parse AsyncTwitchConfig! {e}");
            }
        }

        private void CorrectConfigSettings()
        {
            if (BackgroundPadding < 0)
                BackgroundPadding = 0;
            if (MaxChatLines < 1)
                MaxChatLines = 1;

            ImportAsyncTwitchConfig();

            if (TwitchOAuthToken != String.Empty && !TwitchOAuthToken.StartsWith("oauth:"))
                TwitchOAuthToken = "oauth:" + TwitchOAuthToken;

            if (TwitchChannelName.Length > 0)
            {
                if (TwitchChannelName.Contains("/"))
                {
                    var tmpChannelName = TwitchChannelName.TrimEnd('/').Split('/').Last();
                    Plugin.Log($"Changing twitch channel to {tmpChannelName}");
                    TwitchChannelName = tmpChannelName;
                    Save();
                }
                TwitchChannelName = TwitchChannelName.ToLower().Replace(" ", "");
            }
        }

        private void ConfigWatcherOnChanged(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            if (_saving)
            {
                _saving = false;
                return;
            }

            Load();

            if (ConfigChangedEvent != null)
            {
                ConfigChangedEvent(this);
            }
        }
    }
}