using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using ONI_MP.DebugTools;

namespace ONI_MP.Menus
{
    public class WorkshopInstaller : MonoBehaviour
    {
        private static WorkshopInstaller instance;
        private Dictionary<PublishedFileId_t, InstallOperation> activeInstalls = new Dictionary<PublishedFileId_t, InstallOperation>();

        private struct InstallOperation
        {
            public PublishedFileId_t FileId;
            public Action<string> OnReady;
            public Action<string> OnError;
            public bool IsComplete;
        }

        public static WorkshopInstaller Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject installerObject = new GameObject("WorkshopInstaller");
                    DontDestroyOnLoad(installerObject);
                    instance = installerObject.AddComponent<WorkshopInstaller>();
                }
                return instance;
            }
        }

        void Update()
        {
            // Necessário para callbacks da Steam funcionarem
            if (SteamManager.Initialized)
            {
                SteamAPI.RunCallbacks();
            }
        }

        /// <summary>
        /// Instala um mod do Workshop automaticamente
        /// </summary>
        public void InstallWorkshopItem(string modId, Action<string> onReady, Action<string> onError)
        {
            if (!SteamManager.Initialized)
            {
                onError?.Invoke("Steam não inicializada");
                return;
            }

            // Converte string ID para PublishedFileId_t
            if (!ulong.TryParse(modId, out ulong fileIdULong))
            {
                onError?.Invoke($"ID de mod inválido: {modId}");
                return;
            }

            PublishedFileId_t fileId = new PublishedFileId_t(fileIdULong);
            DebugConsole.Log($"[WorkshopInstaller] Iniciando instalação do mod {modId}");

            StartCoroutine(InstallWorkshopItemCoroutine(fileId, onReady, onError));
        }

        /// <summary>
        /// Instala múltiplos mods em sequência
        /// </summary>
        public void InstallMultipleItems(string[] modIds, Action<int, int> onProgress, Action<string[]> onComplete, Action<string> onError)
        {
            StartCoroutine(InstallMultipleItemsCoroutine(modIds, onProgress, onComplete, onError));
        }

        private IEnumerator InstallMultipleItemsCoroutine(string[] modIds, Action<int, int> onProgress, Action<string[]> onComplete, Action<string> onError)
        {
            List<string> installedPaths = new List<string>();
            int completed = 0;
            bool hasError = false;

            foreach (string modId in modIds)
            {
                if (hasError) break;

                bool installSuccess = false;
                string installPath = "";
                string installError = "";

                InstallWorkshopItem(modId,
                    onReady: path => {
                        installSuccess = true;
                        installPath = path;
                    },
                    onError: err => {
                        installError = err;
                    }
                );

                // Espera a instalação terminar
                yield return new WaitUntil(() => installSuccess || !string.IsNullOrEmpty(installError));

                if (installSuccess)
                {
                    installedPaths.Add(installPath);
                    DebugConsole.Log($"[WorkshopInstaller] Mod {modId} instalado com sucesso");
                }
                else
                {
                    DebugConsole.LogWarning($"[WorkshopInstaller] Falha ao instalar mod {modId}: {installError}");
                    hasError = true;
                    onError?.Invoke($"Falha na instalação de {modId}: {installError}");
                    break;
                }

                completed++;
                onProgress?.Invoke(completed, modIds.Length);

                // Pequena pausa entre instalações
                yield return new WaitForSeconds(0.5f);
            }

            if (!hasError)
            {
                onComplete?.Invoke(installedPaths.ToArray());
            }
        }

        private IEnumerator InstallWorkshopItemCoroutine(PublishedFileId_t fileId, Action<string> onReady, Action<string> onError)
        {
            // Verifica se já está instalado primeiro
            uint currentState = SteamUGC.GetItemState(fileId);
            bool alreadyInstalled = (currentState & (uint)EItemState.k_EItemStateInstalled) != 0;
            bool needsUpdate = (currentState & (uint)EItemState.k_EItemStateNeedsUpdate) != 0;

            if (alreadyInstalled && !needsUpdate)
            {
                // Já instalado, só pega o caminho
                string existingPath = GetInstalledItemPath(fileId);
                if (!string.IsNullOrEmpty(existingPath))
                {
                    DebugConsole.Log($"[WorkshopInstaller] Mod {fileId} já estava instalado em: {existingPath}");
                    onReady?.Invoke(existingPath);
                    yield break;
                }
            }

            // 1) Subscribe
            var subscribeCall = SteamUGC.SubscribeItem(fileId);
            var subscribeResult = new CallResult<RemoteStorageSubscribePublishedFileResult_t>();
            bool subscribeDone = false;
            RemoteStorageSubscribePublishedFileResult_t subData = default;
            bool subIOFailure = false;

            subscribeResult.Set(subscribeCall, (data, ioFailure) =>
            {
                subData = data;
                subIOFailure = ioFailure;
                subscribeDone = true;
            });

            // Espera callback do subscribe
            float timeoutTime = Time.time + 30f; // 30 segundos timeout
            while (!subscribeDone && Time.time < timeoutTime)
            {
                yield return null;
            }

            if (!subscribeDone)
            {
                onError?.Invoke($"Timeout ao assinar item {fileId}");
                yield break;
            }

            if (subIOFailure || subData.m_eResult != EResult.k_EResultOK)
            {
                onError?.Invoke($"Falha ao assinar item {fileId}. Result={subData.m_eResult}");
                yield break;
            }

            DebugConsole.Log($"[WorkshopInstaller] Mod {fileId} assinado com sucesso");

            // 2) Força download
            SteamUGC.DownloadItem(fileId, true);

            // 3) Espera até ficar instalado
            timeoutTime = Time.time + 60f; // 60 segundos para download
            while (Time.time < timeoutTime)
            {
                uint state = SteamUGC.GetItemState(fileId);

                bool installed = (state & (uint)EItemState.k_EItemStateInstalled) != 0;
                bool updating = (state & (uint)EItemState.k_EItemStateNeedsUpdate) != 0;
                bool downloading = (state & (uint)EItemState.k_EItemStateDownloading) != 0;
                bool downloadPending = (state & (uint)EItemState.k_EItemStateDownloadPending) != 0;

                if (updating)
                {
                    SteamUGC.DownloadItem(fileId, true);
                }

                if (installed && !updating && !downloading && !downloadPending)
                {
                    break;
                }

                yield return new WaitForSeconds(0.25f);
            }

            // 4) Pega a pasta onde o Steam instalou
            string finalPath = GetInstalledItemPath(fileId);
            if (!string.IsNullOrEmpty(finalPath))
            {
                DebugConsole.Log($"[WorkshopInstaller] Mod {fileId} instalado em: {finalPath}");
                onReady?.Invoke(finalPath);
            }
            else
            {
                onError?.Invoke($"Item {fileId} instalado, mas não consegui obter o diretório");
            }
        }

        private string GetInstalledItemPath(PublishedFileId_t fileId)
        {
            try
            {
                ulong sizeOnDisk;
                uint timeStamp;
                string folder;
                bool ok = SteamUGC.GetItemInstallInfo(fileId, out sizeOnDisk, out folder, 1024, out timeStamp);

                if (ok && !string.IsNullOrEmpty(folder))
                {
                    return folder;
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[WorkshopInstaller] Erro ao obter caminho do item {fileId}: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Ativa um mod no sistema do jogo após instalação
        /// </summary>
        public bool ActivateInstalledMod(string modId, string installedPath)
        {
            try
            {
                DebugConsole.Log($"[WorkshopInstaller] Tentando ativar mod {modId} do caminho: {installedPath}");

                var modManager = Global.Instance?.modManager;
                if (modManager == null)
                {
                    DebugConsole.LogWarning("[WorkshopInstaller] ModManager não disponível");
                    return false;
                }

                // Força recarregamento do mod manager para detectar novos mods
                try
                {
                    modManager.Report(null);
                }
                catch (Exception ex)
                {
                    DebugConsole.LogWarning($"[WorkshopInstaller] Erro ao recarregar mod manager: {ex.Message}");
                }

                // Procura o mod na lista de mods disponíveis
                foreach (var mod in modManager.mods)
                {
                    if (mod?.label != null && mod.label.id == modId)
                    {
                        // Verifica se o mod está habilitado usando o método correto
                        bool isEnabled = modManager.IsModEnabled(mod.label);

                        if (!isEnabled)
                        {
                            try
                            {
                                // Ativa o mod usando o método correto
                                modManager.EnableMod(mod.label, true, null);

                                // Salva as mudanças
                                modManager.Save();

                                DebugConsole.Log($"[WorkshopInstaller] Mod {modId} ({mod.title}) ativado com sucesso!");
                                return true;
                            }
                            catch (Exception ex)
                            {
                                DebugConsole.LogWarning($"[WorkshopInstaller] Erro ao ativar mod {modId}: {ex.Message}");
                                return false;
                            }
                        }
                        else
                        {
                            DebugConsole.Log($"[WorkshopInstaller] Mod {modId} ({mod.title}) já estava ativo");
                            return true;
                        }
                    }
                }

                // Mod não encontrado na lista atual, pode ter sido instalado mas não carregado ainda
                DebugConsole.Log($"[WorkshopInstaller] Mod {modId} instalado mas não aparece na lista ainda");
                DebugConsole.Log("[WorkshopInstaller] Tentando recarregar lista de mods...");

                // Tenta recarregar novamente após instalação
                try
                {
                    modManager.Report(null);

                    // Segunda tentativa de encontrar o mod
                    foreach (var mod in modManager.mods)
                    {
                        if (mod?.label != null && mod.label.id == modId)
                        {
                            modManager.EnableMod(mod.label, true, null);
                            modManager.Save();
                            DebugConsole.Log($"[WorkshopInstaller] Mod {modId} encontrado na segunda tentativa e ativado!");
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    DebugConsole.LogWarning($"[WorkshopInstaller] Erro no recarregamento: {ex.Message}");
                }

                DebugConsole.Log($"[WorkshopInstaller] Mod {modId} instalado com sucesso, mas pode necessitar reinicialização do jogo para aparecer");
                return true; // Consideramos sucesso se a instalação funcionou
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[WorkshopInstaller] Erro ao ativar mod {modId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Instala e ativa um mod automaticamente
        /// </summary>
        public void InstallAndActivateMod(string modId, Action<bool> onComplete)
        {
            InstallWorkshopItem(modId,
                onReady: installedPath => {
                    // Pequena pausa para garantir que o sistema detectou o mod
                    StartCoroutine(DelayedActivation(modId, installedPath, onComplete));
                },
                onError: error => {
                    DebugConsole.LogWarning($"[WorkshopInstaller] Falha na instalação automática: {error}");
                    onComplete?.Invoke(false);
                }
            );
        }

        private IEnumerator DelayedActivation(string modId, string installedPath, Action<bool> onComplete)
        {
            yield return new WaitForSeconds(1f); // Pausa para sistema detectar o mod

            bool activated = ActivateInstalledMod(modId, installedPath);
            onComplete?.Invoke(activated);
        }
    }
}