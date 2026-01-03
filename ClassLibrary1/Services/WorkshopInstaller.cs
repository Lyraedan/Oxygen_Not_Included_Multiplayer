using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using ONI_MP.DebugTools;

namespace ONI_MP.Services
{
    public class WorkshopInstaller : MonoBehaviour
    {
        private static WorkshopInstaller instance;
        private Dictionary<PublishedFileId_t, InstallOperation> activeInstalls = new Dictionary<PublishedFileId_t, InstallOperation>();

        // CallResults as member variables to prevent garbage collection (Steamworks.NET best practice)
        private CallResult<RemoteStorageSubscribePublishedFileResult_t> m_SubscribeItemCallResult;
        private CallResult<RemoteStorageUnsubscribePublishedFileResult_t> m_UnsubscribeItemCallResult;

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

        void Awake()
        {
            // Initialize CallResults to prevent garbage collection (Steamworks.NET best practice)
            if (SteamManager.Initialized)
            {
                m_SubscribeItemCallResult = CallResult<RemoteStorageSubscribePublishedFileResult_t>.Create(OnSubscribeItemResult);
                m_UnsubscribeItemCallResult = CallResult<RemoteStorageUnsubscribePublishedFileResult_t>.Create(OnUnsubscribeItemResult);
            }
            else
            {
                DebugConsole.LogWarning("[WorkshopInstaller] Steam not initialized during Awake");
            }
        }

        void Update()
        {
            // Necessário para callbacks da Steam funcionarem
            if (SteamManager.Initialized)
            {
                SteamAPI.RunCallbacks();
            }

            // Check for pending activations every 5 seconds
            if (Time.time - lastActivationCheck > 5f)
            {
                CheckForPendingActivations();
                lastActivationCheck = Time.time;
            }
        }

        private float lastActivationCheck = 0f;
        private Dictionary<string, float> pendingActivations = new Dictionary<string, float>();

        // Current operation tracking for improved callback handling
        private PublishedFileId_t currentSubscriptionId = PublishedFileId_t.Invalid;
        private Action<string> currentSubscriptionOnReady;
        private Action<string> currentSubscriptionOnError;

        /// <summary>
        /// Verifica se há mods instalados aguardando ativação
        /// </summary>
        private void CheckForPendingActivations()
        {
            if (pendingActivations.Count == 0) return;

            var modManager = Global.Instance?.modManager;
            if (modManager == null) return;

            var keysToRemove = new List<string>();

            foreach (var kvp in pendingActivations)
            {
                string modId = kvp.Key;
                float pendingTime = kvp.Value;

                // Remove mods pending for more than 2 minutes
                if (Time.time - pendingTime > 120f)
                {
                    DebugConsole.LogWarning($"[WorkshopInstaller] Timeout: Mod {modId} removed from pending activation queue");
                    keysToRemove.Add(modId);
                    continue;
                }

                // Try to activate pending mods
                try
                {
                    modManager.Report(null); // Refresh list

                    foreach (var mod in modManager.mods)
                    {
                        if (mod?.label != null && (mod.label.id == modId || mod.label.id.Contains(modId)))
                        {
                            if (!modManager.IsModEnabled(mod.label))
                            {
                                modManager.EnableMod(mod.label, true, null);
                                modManager.Save();
                            }
                            keysToRemove.Add(modId);
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    DebugConsole.LogWarning($"[WorkshopInstaller] Error activating pending mod {modId}: {ex.Message}");
                }
            }

            // Remove mods processados
            foreach (string key in keysToRemove)
            {
                pendingActivations.Remove(key);
            }
        }

        /// <summary>
        /// Detailed callback for subscribe operations (Steamworks.NET best practice)
        /// </summary>
        private void OnSubscribeItemResult(RemoteStorageSubscribePublishedFileResult_t result, bool bIOFailure)
        {
            try
            {
                if (bIOFailure)
                {
                    DebugConsole.LogWarning($"[WorkshopInstaller] Subscribe failed due to IO failure for {result.m_nPublishedFileId}");
                    currentSubscriptionOnError?.Invoke($"IO failure during subscription of {result.m_nPublishedFileId}");
                    return;
                }

                if (result.m_eResult == EResult.k_EResultOK)
                {
                    // Continue with installation process - handled by coroutine waiting for callback
                }
                else
                {
                    string errorMsg = $"Subscribe failed with result: {result.m_eResult}";
                    DebugConsole.LogWarning($"[WorkshopInstaller] {errorMsg} for {result.m_nPublishedFileId}");

                    // Common error explanations
                    switch (result.m_eResult)
                    {
                        case EResult.k_EResultFileNotFound:
                            errorMsg += " (Mod not found - may have been removed)";
                            break;
                        case EResult.k_EResultAccessDenied:
                            errorMsg += " (Access denied - mod may be private)";
                            break;
                        case EResult.k_EResultLimitExceeded:
                            errorMsg += " (Rate limit exceeded - try again later)";
                            break;
                        case EResult.k_EResultTimeout:
                            errorMsg += " (Steam timeout - check connection)";
                            break;
                        default:
                            errorMsg += " (Unknown error)";
                            break;
                    }

                    currentSubscriptionOnError?.Invoke(errorMsg);
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[WorkshopInstaller] Exception in subscribe callback: {ex.Message}");
                currentSubscriptionOnError?.Invoke($"Callback exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Callback for unsubscribe operations
        /// </summary>
        private void OnUnsubscribeItemResult(RemoteStorageUnsubscribePublishedFileResult_t result, bool bIOFailure)
        {
            if (bIOFailure || result.m_eResult != EResult.k_EResultOK)
            {
                DebugConsole.LogWarning($"[WorkshopInstaller] Unsubscribe failed: IO={bIOFailure}, Result={result.m_eResult}");
            }
        }

        /// <summary>
        /// Adds a mod to the pending activation queue
        /// </summary>
        public void AddToPendingActivation(string modId)
        {
            if (!pendingActivations.ContainsKey(modId))
            {
                pendingActivations[modId] = Time.time;
            }
        }

        /// <summary>
        /// Apenas faz subscribe de um mod - Steam cuida da instalação automaticamente
        /// </summary>
        public void SubscribeToWorkshopItem(string modId, Action<string> onSuccess, Action<string> onError)
        {
            if (!SteamManager.Initialized)
            {
                DebugConsole.LogWarning($"[WorkshopInstaller] Steam not initialized for mod {modId}");
                onError?.Invoke("Steam not initialized");
                return;
            }

            // Convert string ID to PublishedFileId_t
            if (!ulong.TryParse(modId, out ulong fileIdULong))
            {
                DebugConsole.LogWarning($"[WorkshopInstaller] Invalid mod ID: {modId}");
                onError?.Invoke($"Invalid mod ID: {modId}");
                return;
            }

            PublishedFileId_t fileId = new PublishedFileId_t(fileIdULong);
            // Steam will handle installation automatically

            StartCoroutine(SubscribeOnlyCoroutine(fileId, modId, onSuccess, onError));
        }

        /// <summary>
        /// Coroutine simples que apenas faz subscribe - Steam cuida do resto
        /// </summary>
        private IEnumerator SubscribeOnlyCoroutine(PublishedFileId_t fileId, string modId, Action<string> onSuccess, Action<string> onError)
        {
            // Diagnóstico inicial
            DiagnoseSteamModState(fileId, modId);

            // Verifica se já está subscrito
            uint currentState = SteamUGC.GetItemState(fileId);
            bool alreadySubscribed = (currentState & (uint)EItemState.k_EItemStateSubscribed) != 0;

            if (alreadySubscribed)
            {
                DebugConsole.Log($"[WorkshopInstaller] ✅ Mod {modId} já está subscrito - Steam vai gerenciar instalação");
                onSuccess?.Invoke(modId);
                yield break;
            }

            // Faz subscribe simples
            DebugConsole.Log($"[WorkshopInstaller] 📝 Fazendo subscribe do mod {modId}...");

            // Initialize CallResults if not done yet
            if (m_SubscribeItemCallResult == null && SteamManager.Initialized)
            {
                m_SubscribeItemCallResult = CallResult<RemoteStorageSubscribePublishedFileResult_t>.Create(OnSubscribeItemResult);
            }

            if (m_SubscribeItemCallResult == null)
            {
                onError?.Invoke("Steam callback system not available");
                yield break;
            }

            // Faz a chamada de subscribe
            SteamAPICall_t subscribeCall = SteamUGC.SubscribeItem(fileId);
            DebugConsole.Log($"[WorkshopInstaller] 📡 Subscribe call made: {subscribeCall} para mod {modId}");

            m_SubscribeItemCallResult.Set(subscribeCall);

            // Aguarda resultado do subscribe
            float timeoutTime = Time.time + 30f;
            while (Time.time < timeoutTime)
            {
                uint subState = SteamUGC.GetItemState(fileId);
                bool subscribed = (subState & (uint)EItemState.k_EItemStateSubscribed) != 0;

                if (subscribed)
                {
                    DebugConsole.Log($"[WorkshopInstaller] ✅ Mod {modId} subscrito com sucesso! Steam fará download automaticamente.");
                    onSuccess?.Invoke(modId);
                    yield break;
                }

                yield return null;
            }

            // Timeout
            DebugConsole.LogWarning($"[WorkshopInstaller] ⏰ Timeout ao fazer subscribe do mod {modId}");
            onError?.Invoke($"Timeout ao fazer subscribe do mod {modId}");
        }


        /// <summary>
        /// Diagnostica o estado atual de um mod no Steam para debug
        /// </summary>
        private void DiagnoseSteamModState(PublishedFileId_t fileId, string modId)
        {
            try
            {
                uint currentState = SteamUGC.GetItemState(fileId);

                bool subscribed = (currentState & (uint)EItemState.k_EItemStateSubscribed) != 0;
                bool installed = (currentState & (uint)EItemState.k_EItemStateInstalled) != 0;
                bool downloading = (currentState & (uint)EItemState.k_EItemStateDownloading) != 0;
                bool downloadPending = (currentState & (uint)EItemState.k_EItemStateDownloadPending) != 0;
                bool needsUpdate = (currentState & (uint)EItemState.k_EItemStateNeedsUpdate) != 0;
                bool legacy = (currentState & (uint)EItemState.k_EItemStateLegacyItem) != 0;

                DebugConsole.Log($"[WorkshopInstaller] 📋 Diagnóstico inicial do mod {modId}:");
                DebugConsole.Log($"[WorkshopInstaller]   • Subscribed: {(subscribed ? "✅" : "❌")}");
                DebugConsole.Log($"[WorkshopInstaller]   • Installed: {(installed ? "✅" : "❌")}");
                DebugConsole.Log($"[WorkshopInstaller]   • Downloading: {(downloading ? "✅" : "❌")}");
                DebugConsole.Log($"[WorkshopInstaller]   • Download Pending: {(downloadPending ? "✅" : "❌")}");
                DebugConsole.Log($"[WorkshopInstaller]   • Needs Update: {(needsUpdate ? "✅" : "❌")}");
                DebugConsole.Log($"[WorkshopInstaller]   • Legacy Item: {(legacy ? "✅" : "❌")}");
                DebugConsole.Log($"[WorkshopInstaller]   • Raw State: {currentState}");

                // Tenta obter informações de instalação se disponíveis
                string currentPath = GetInstalledItemPath(fileId);
                if (!string.IsNullOrEmpty(currentPath))
                {
                    DebugConsole.Log($"[WorkshopInstaller]   • Current Path: {currentPath}");
                }
                else
                {
                    DebugConsole.Log($"[WorkshopInstaller]   • Current Path: ❌ Não disponível");
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[WorkshopInstaller] Erro no diagnóstico do mod {modId}: {ex.Message}");
            }
        }





        /// <summary>
        /// Ativa um mod no sistema do jogo após instalação
        /// </summary>
        public bool ActivateInstalledMod(string modId, string installedPath)
        {
            try
            {
                DebugConsole.Log($"[WorkshopInstaller] 🔄 Iniciando ativação automática do mod {modId}");
                DebugConsole.Log($"[WorkshopInstaller] Caminho de instalação: {installedPath}");

                var modManager = Global.Instance?.modManager;
                if (modManager == null)
                {
                    DebugConsole.LogWarning("[WorkshopInstaller] ❌ ModManager não disponível - aguardando sistema carregar...");
                    return false;
                }

                DebugConsole.Log($"[WorkshopInstaller] 📋 Total de mods carregados no sistema: {modManager.mods?.Count ?? 0}");

                // Força recarregamento do mod manager para detectar novos mods recém-instalados
                try
                {
                    DebugConsole.Log("[WorkshopInstaller] 🔄 Recarregando lista de mods para detectar novos...");
                    modManager.Report(null);
                    DebugConsole.Log($"[WorkshopInstaller] ✅ Recarregamento completo. Mods disponíveis: {modManager.mods?.Count ?? 0}");
                }
                catch (Exception ex)
                {
                    DebugConsole.LogWarning($"[WorkshopInstaller] ⚠️ Erro ao recarregar mod manager: {ex.Message}");
                }

                // Lista todos os mods para debug
                if (modManager.mods != null)
                {
                    DebugConsole.Log("[WorkshopInstaller] 📜 Lista de mods detectados:");
                    int count = 0;
                    foreach (var mod in modManager.mods)
                    {
                        if (mod?.label != null)
                        {
                            count++;
                            string status = modManager.IsModEnabled(mod.label) ? "✅ ATIVO" : "⚪ INATIVO";
                            DebugConsole.Log($"[WorkshopInstaller]   [{count}] {mod.label.id} - {mod.title} - {status}");
                        }
                    }
                }

                // Procura o mod na lista usando múltiplos métodos de busca
                DebugConsole.Log($"[WorkshopInstaller] 🔍 Procurando mod com ID: '{modId}'");

                // Primeiro, tenta busca mais agressiva para mods legacy
                foreach (var mod in modManager.mods)
                {
                    if (mod?.label != null)
                    {
                        string actualId = mod.label.id;
                        string staticId = mod.label.defaultStaticID;
                        string title = mod.title ?? "";

                        // Log detalhado para debug de mods legacy
                        DebugConsole.Log($"[WorkshopInstaller] 🔍 Verificando mod: '{title}' | ID: '{actualId}' | StaticID: '{staticId}'");

                        // Para mods legacy, o ID pode estar no title ou ser completamente diferente
                        bool isMatch = false;

                        // 1. Match exato com IDs
                        if (actualId == modId || staticId == modId)
                        {
                            isMatch = true;
                            DebugConsole.Log($"[WorkshopInstaller] ✅ Match exato por ID!");
                        }
                        // 2. ID contém ou está contido
                        else if (actualId.Contains(modId) || staticId.Contains(modId) ||
                                modId.Contains(actualId) || modId.Contains(staticId))
                        {
                            isMatch = true;
                            DebugConsole.Log($"[WorkshopInstaller] ✅ Match parcial por ID!");
                        }
                        // 3. Para mods legacy: procura o ID no título
                        else if (title.Contains(modId))
                        {
                            isMatch = true;
                            DebugConsole.Log($"[WorkshopInstaller] ✅ Match por título (mod legacy)!");
                        }
                        // 4. Para mods legacy: procura números similares
                        else if (!string.IsNullOrEmpty(actualId) && !string.IsNullOrEmpty(modId))
                        {
                            // Extrai números do actualId e compara
                            var actualNumbers = System.Text.RegularExpressions.Regex.Matches(actualId, @"\d+");
                            var searchNumbers = System.Text.RegularExpressions.Regex.Matches(modId, @"\d+");

                            foreach (System.Text.RegularExpressions.Match actualNum in actualNumbers)
                            {
                                foreach (System.Text.RegularExpressions.Match searchNum in searchNumbers)
                                {
                                    if (actualNum.Value == searchNum.Value && actualNum.Value.Length >= 8) // IDs grandes
                                    {
                                        isMatch = true;
                                        DebugConsole.Log($"[WorkshopInstaller] ✅ Match numérico: {actualNum.Value}!");
                                        break;
                                    }
                                }
                                if (isMatch) break;
                            }
                        }

                        if (isMatch)
                        {
                            DebugConsole.Log($"[WorkshopInstaller] 🎯 Mod encontrado: '{title}'");
                            DebugConsole.Log($"[WorkshopInstaller]   • ID interno: {actualId}");
                            DebugConsole.Log($"[WorkshopInstaller]   • Static ID: {staticId}");

                            // Verifica se o mod está habilitado
                            bool isEnabled = modManager.IsModEnabled(mod.label);
                            DebugConsole.Log($"[WorkshopInstaller] Status atual: {(isEnabled ? "✅ JÁ ATIVO" : "⚪ INATIVO")}");

                            if (!isEnabled)
                            {
                                try
                                {
                                    DebugConsole.Log($"[WorkshopInstaller] 🔧 Ativando mod {title}...");

                                    // Ativa o mod usando o método correto do ONI
                                    modManager.EnableMod(mod.label, true, null);

                                    // Salva as mudanças imediatamente
                                    modManager.Save();

                                    // Confirma ativação
                                    bool nowEnabled = modManager.IsModEnabled(mod.label);
                                    if (nowEnabled)
                                    {
                                        DebugConsole.Log($"[WorkshopInstaller] ✅ Mod {modId} ({title}) ATIVADO COM SUCESSO!");
                                        return true;
                                    }
                                    else
                                    {
                                        DebugConsole.LogWarning($"[WorkshopInstaller] ⚠️ Mod {modId} foi processado mas ainda não aparece como ativo");
                                        return false;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    DebugConsole.LogWarning($"[WorkshopInstaller] ❌ Erro ao ativar mod {modId}: {ex.Message}");
                                    return false;
                                }
                            }
                            else
                            {
                                DebugConsole.Log($"[WorkshopInstaller] ✅ Mod {modId} ({title}) já estava ativo!");
                                return true;
                            }
                        }
                    }
                }

                // Mod não encontrado - tenta estratégia mais agressiva
                DebugConsole.LogWarning($"[WorkshopInstaller] ❌ Mod {modId} não encontrado na primeira busca");
                DebugConsole.Log("[WorkshopInstaller] 🔄 Tentando recarregamento mais agressivo...");

                // Tenta múltiplos recarregamentos
                for (int attempt = 1; attempt <= 3; attempt++)
                {
                    try
                    {
                        DebugConsole.Log($"[WorkshopInstaller] Tentativa {attempt}/3 de recarregamento...");

                        // Wait a bit before retry
                        if (attempt > 1)
                        {
                            System.Threading.Thread.Sleep(1000 * attempt); // Progressive delay
                        }

                        modManager.Report(null);

                        // Procura novamente
                        foreach (var mod in modManager.mods)
                        {
                            if (mod?.label != null && (mod.label.id == modId || mod.label.id.Contains(modId)))
                            {
                                DebugConsole.Log($"[WorkshopInstaller] ✅ Mod {modId} encontrado na tentativa {attempt}!");

                                if (!modManager.IsModEnabled(mod.label))
                                {
                                    modManager.EnableMod(mod.label, true, null);
                                    modManager.Save();
                                }

                                DebugConsole.Log($"[WorkshopInstaller] ✅ Mod {modId} ativado com sucesso na tentativa {attempt}!");
                                return true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugConsole.LogWarning($"[WorkshopInstaller] Erro na tentativa {attempt}: {ex.Message}");
                    }
                }

                DebugConsole.LogWarning($"[WorkshopInstaller] ⚠️ Mod {modId} foi instalado mas não conseguiu ser ativado automaticamente");
                DebugConsole.Log("[WorkshopInstaller] 🔄 Adicionando à fila de ativação pendente para tentar novamente em background");

                // Adiciona à fila de ativação pendente para tentar continuamente em background
                AddToPendingActivation(modId);

                return true; // Consideramos sucesso parcial - sistema continuará tentando
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[WorkshopInstaller] ❌ Erro crítico ao ativar mod {modId}: {ex.Message}");
                DebugConsole.LogWarning($"[WorkshopInstaller] Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Subscribe e monitora ativação automática de um mod (nova abordagem)
        /// </summary>
        public void SubscribeAndActivateMod(string modId, Action<bool> onComplete)
        {
            DebugConsole.Log($"[WorkshopInstaller] 🚀 Starting subscribe and activate for mod {modId}");

            SubscribeToWorkshopItem(modId,
                onSuccess: subscribedModId => {
                    DebugConsole.Log($"[WorkshopInstaller] ✅ Successfully subscribed to mod {subscribedModId} - Steam will handle installation");

                    // Start monitoring Steam's automatic installation and activation
                    StartCoroutine(MonitorSubscribeAndActivate(modId, onComplete));
                },
                onError: error => {
                    DebugConsole.LogWarning($"[WorkshopInstaller] ❌ Subscribe failed for mod {modId}: {error}");
                    onComplete?.Invoke(false);
                }
            );
        }

        /// <summary>
        /// Monitors Steam installation and attempts activation when ready
        /// </summary>
        private System.Collections.IEnumerator MonitorSubscribeAndActivate(string modId, Action<bool> onComplete)
        {
            if (!ulong.TryParse(modId, out ulong fileIdULong))
            {
                DebugConsole.LogWarning($"[WorkshopInstaller] ❌ Invalid mod ID for monitoring: {modId}");
                onComplete?.Invoke(false);
                yield break;
            }

            PublishedFileId_t fileId = new PublishedFileId_t(fileIdULong);
            DebugConsole.Log($"[WorkshopInstaller] 👀 Monitoring Steam installation for mod {modId}...");

            float timeoutTime = Time.time + 300f; // 5 minutes max
            bool activationAttempted = false;

            while (Time.time < timeoutTime)
            {
                uint currentState = SteamUGC.GetItemState(fileId);
                bool subscribed = (currentState & (uint)EItemState.k_EItemStateSubscribed) != 0;
                bool installed = (currentState & (uint)EItemState.k_EItemStateInstalled) != 0;
                bool downloading = (currentState & (uint)EItemState.k_EItemStateDownloading) != 0;

                if (subscribed && installed && !downloading && !activationAttempted)
                {
                    DebugConsole.Log($"[WorkshopInstaller] ✅ Steam completed installation - attempting activation for mod {modId}");
                    activationAttempted = true;

                    // Small pause to let Steam finish processing
                    yield return new WaitForSeconds(2f);

                    string installedPath = GetInstalledItemPath(fileId);
                    bool activated = ActivateInstalledMod(modId, installedPath);

                    DebugConsole.Log($"[WorkshopInstaller] {(activated ? "✅" : "⚠️")} Activation result for mod {modId}: {activated}");
                    onComplete?.Invoke(activated);
                    yield break;
                }

                if (!subscribed)
                {
                    DebugConsole.LogWarning($"[WorkshopInstaller] ⚠️ Lost subscription to mod {modId}");
                    onComplete?.Invoke(false);
                    yield break;
                }

                yield return new WaitForSeconds(5f);
            }

            DebugConsole.LogWarning($"[WorkshopInstaller] ⏰ Timeout waiting for Steam installation of mod {modId}");
            onComplete?.Invoke(false);
        }

        /// <summary>
        /// Obtém o caminho de instalação de um mod Steam
        /// </summary>
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

            return "";
        }

        /// <summary>
        /// Diagnóstica problemas comuns de instalação de mods
        /// </summary>
        public static void DiagnoseInstallationProblems()
        {
            try
            {
                DebugConsole.Log("[WorkshopInstaller] 🔧 === DIAGNÓSTICO DO SISTEMA DE INSTALAÇÃO ===");

                // 1. Verifica inicialização do Steam
                if (!SteamManager.Initialized)
                {
                    DebugConsole.LogWarning("[WorkshopInstaller] ❌ PROBLEMA: Steam não está inicializada!");
                    DebugConsole.Log("[WorkshopInstaller] 💡 Solução: Reinicie o jogo via Steam");
                    return;
                }
                else
                {
                    DebugConsole.Log("[WorkshopInstaller] ✅ Steam inicializada corretamente");
                }

                // 2. Verifica se callbacks estão funcionando
                try
                {
                    SteamAPI.RunCallbacks();
                    DebugConsole.Log("[WorkshopInstaller] ✅ Callbacks do Steam funcionando");
                }
                catch (Exception ex)
                {
                    DebugConsole.LogWarning($"[WorkshopInstaller] ❌ PROBLEMA: Callbacks do Steam falhando: {ex.Message}");
                }

                // 3. Verifica status da rede Steam
                bool isConnected = SteamUser.BLoggedOn();
                DebugConsole.Log($"[WorkshopInstaller] {(isConnected ? "✅" : "❌")} Status Steam: {(isConnected ? "Conectado" : "Desconectado")}");

                // 4. Verifica ModManager
                var modManager = Global.Instance?.modManager;
                if (modManager == null)
                {
                    DebugConsole.LogWarning("[WorkshopInstaller] ❌ PROBLEMA: ModManager não disponível");
                    DebugConsole.Log("[WorkshopInstaller] 💡 Aguarde o jogo carregar completamente");
                }
                else
                {
                    DebugConsole.Log($"[WorkshopInstaller] ✅ ModManager disponível ({modManager.mods?.Count ?? 0} mods carregados)");
                }

                // 5. Verifica instance do WorkshopInstaller
                if (instance == null)
                {
                    DebugConsole.LogWarning("[WorkshopInstaller] ⚠️ WorkshopInstaller não foi criado ainda");
                }
                else
                {
                    DebugConsole.Log($"[WorkshopInstaller] ✅ WorkshopInstaller ativo, {instance.activeInstalls.Count} instalações ativas");
                }

                DebugConsole.Log("[WorkshopInstaller] 🔧 === FIM DO DIAGNÓSTICO ===");
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[WorkshopInstaller] Erro durante diagnóstico: {ex.Message}");
            }
        }

        /// <summary>
        /// Verifica estado específico de um mod por ID
        /// </summary>
        public static void CheckSpecificModState(string modId)
        {
            try
            {
                if (!ulong.TryParse(modId, out ulong fileIdULong))
                {
                    DebugConsole.LogWarning($"[WorkshopInstaller] ❌ ID inválido: {modId}");
                    return;
                }

                PublishedFileId_t fileId = new PublishedFileId_t(fileIdULong);
                DebugConsole.Log($"[WorkshopInstaller] 🔍 === VERIFICAÇÃO DO MOD {modId} ===");

                if (!SteamManager.Initialized)
                {
                    DebugConsole.LogWarning("[WorkshopInstaller] ❌ Steam não inicializada");
                    return;
                }

                // Diagnóstico do estado do mod
                Instance.DiagnoseSteamModState(fileId, modId);

                // Verifica se mod está no sistema do jogo
                var modManager = Global.Instance?.modManager;
                if (modManager != null)
                {
                    bool foundInGame = false;
                    foreach (var mod in modManager.mods)
                    {
                        if (mod?.label != null &&
                            (mod.label.id == modId || mod.label.id.Contains(modId) ||
                             mod.label.defaultStaticID == modId || mod.label.defaultStaticID.Contains(modId)))
                        {
                            foundInGame = true;
                            bool isEnabled = modManager.IsModEnabled(mod.label);
                            DebugConsole.Log($"[WorkshopInstaller] ✅ Mod encontrado no jogo: {mod.title}");
                            DebugConsole.Log($"[WorkshopInstaller]   • Status: {(isEnabled ? "✅ ATIVO" : "⚪ INATIVO")}");
                            DebugConsole.Log($"[WorkshopInstaller]   • ID do jogo: {mod.label.id}");
                            break;
                        }
                    }

                    if (!foundInGame)
                    {
                        DebugConsole.LogWarning($"[WorkshopInstaller] ❌ Mod {modId} NÃO encontrado na lista do jogo");
                        DebugConsole.Log("[WorkshopInstaller] 💡 Pode precisar recarregar mods ou reiniciar o jogo");
                    }
                }

                DebugConsole.Log($"[WorkshopInstaller] 🔍 === FIM VERIFICAÇÃO {modId} ===");
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[WorkshopInstaller] Erro ao verificar mod {modId}: {ex.Message}");
            }
        }
    }
}