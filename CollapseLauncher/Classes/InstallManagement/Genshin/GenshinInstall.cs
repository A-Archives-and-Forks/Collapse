﻿using CollapseLauncher.GameVersioning;
using CollapseLauncher.InstallManager.Base;
using CollapseLauncher.Interfaces;
using Hi3Helper;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Hi3Helper.Logger;

namespace CollapseLauncher.InstallManager.Genshin
{
    internal class GenshinInstall : InstallManagerBase<GameTypeGenshinVersion>
    {
        #region Override Properties
        protected override int _gameVoiceLanguageID { get => _gameVersionManager.GamePreset.GetVoiceLanguageID(); }
        #endregion

        #region Properties
        protected override string _gameAudioLangListPath
        {
            get
            {
                // If the persistent folder is not exist, then return null
                if (!Directory.Exists(_gameDataPersistentPath)) return null;

                // Try get the file list
                string[] audioPath = Directory.GetFiles(_gameDataPersistentPath, "audio_lang_*", SearchOption.TopDirectoryOnly);
                // If the path is null or has no length, then return null
                if (audioPath == null || audioPath.Length == 0)
                {
                    return null;
                }

                // If not, then return the first path
                return audioPath[0];
            }
        }
        protected override string _gameAudioLangListPathStatic { get => Path.Combine(_gameDataPersistentPath, "audio_lang_14"); }
        private string _gameAudioNewPath { get => Path.Combine(_gameDataPath, "StreamingAssets", "AudioAssets"); }
        private string _gameAudioOldPath { get => Path.Combine(_gameDataPath, "StreamingAssets", "Audio", "GeneratedSoundBanks", "Windows"); }
        #endregion

        public GenshinInstall(UIElement parentUI, IGameVersionCheck GameVersionManager)
            : base(parentUI, GameVersionManager)
        {

        }

        #region Public Methods
        public override async ValueTask<bool> IsPreloadCompleted(CancellationToken token)
        {
            // Get the primary file first check
            List<RegionResourceVersion> resource = _gameVersionManager.GetGamePreloadZip();

            // Sanity Check: throw if resource returns null
            if (resource == null) throw new InvalidOperationException($"You're trying to check this method while preload is not available!");

            bool primaryAsset = resource.All(x =>
            {
                string name = Path.GetFileName(x.path);
                string path = Path.Combine(_gamePath, name);

                return File.Exists(path);
            });

            // Get the second voice pack check
            List<GameInstallPackage> voicePackList = new List<GameInstallPackage>();

            // Add another voice pack that already been installed
            await TryAddOtherInstalledVoicePacks(resource.FirstOrDefault().voice_packs, voicePackList, resource.FirstOrDefault().version);

            // Get the secondary file check
            bool secondaryAsset = voicePackList.All(x => File.Exists(x.PathOutput));

            return (primaryAsset && secondaryAsset) || await base.IsPreloadCompleted(token);
        }
        #endregion

        #region Override Methods - StartPackageInstallationInner
        protected override async Task StartPackageInstallationInner(List<GameInstallPackage> gamePackage = null, bool isOnlyInstallPackage = false, bool doNotDeleteZipExplicit = false)
        {
            if (!isOnlyInstallPackage)
                // Starting from 3.6 update, the Audio files have been moved to "AudioAssets" folder
                EnsureMoveOldToNewAudioDirectory();

            // Run the base installation process
            await base.StartPackageInstallationInner(gamePackage);

            // Then start on processing hdifffiles list and deletefiles list
            await ApplyHdiffListPatch();
            ApplyDeleteFileAction();
        }

        protected void EnsureMoveOldToNewAudioDirectory()
        {
            // Return if the old path doesn't exist
            if (!Directory.Exists(_gameAudioOldPath)) return;

            // If it exists, then enumerate the content of it and do move operation
            int offset = _gameAudioOldPath.Length + 1;
            foreach (string oldPath in Directory.EnumerateFiles(_gameAudioOldPath, "*", SearchOption.AllDirectories))
            {
                string basePath = oldPath.AsSpan().Slice(offset).ToString();
                string newPath = Path.Combine(_gameAudioNewPath, basePath);
                string newFolder = Path.GetDirectoryName(newPath);

                if (!Directory.Exists(newFolder))
                {
                    Directory.CreateDirectory(newFolder);
                }

                FileInfo oldFileInfo = new FileInfo(oldPath);
                oldFileInfo.IsReadOnly = false;
                oldFileInfo.MoveTo(newPath, true);
            }

            try
            {
                // Then if all the files are already moved, delete the old path
                if (Directory.Exists(_gameAudioOldPath))
                {
                    Directory.CreateDirectory(_gameAudioOldPath);
                }
            }
            catch (Exception ex)
            {
                LogWriteLine($"Failed while deleting old audio folder: {_gameAudioOldPath}\r\n{ex}", LogType.Error, true);
            }
        }
        #endregion

        #region Override Methods - UninstallGame
        protected override UninstallGameProperty AssignUninstallFolders()
        {
            string execName = _gameVersionManager.GamePreset.ZoneName switch
            {
                "Global" => "GenshinImpact",
                "Mainland China" => "YuanShen",
                "Bilibili" => "YuanShen",
                "Google Play" => "GenshinImpact",
                _ => throw new NotSupportedException($"Unknown GI Game Region!: {_gameVersionManager.GamePreset.ZoneName}")
            };

            return new UninstallGameProperty
            {
                gameDataFolderName  = $"{execName}_Data",
                foldersToDelete     = new[] { $"{execName}_Data" },
                filesToDelete       = new[] { "HoYoKProtect.sys", "pkg_version", $"{execName}.exe", "UnityPlayer.dll", "config.ini", "^mhyp.*", "^Audio.*" },
                foldersToKeepInData = Array.Empty<string>()
            };
        }
        #endregion
    }
}
