using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONI_MP.Networking.Relay
{
    using System;
    using System.Collections.Generic;

    namespace ONI_MP.Networking.Relay
    {
        public interface ILobby
        {
            /// <summary> Whether the user is currently in a lobby. </summary>
            bool InLobby { get; }

            /// <summary> The ID of the currently joined lobby. </summary>
            string CurrentLobby { get; }

            /// <summary> The maximum number of players allowed in the current lobby. </summary>
            int MaxLobbySize { get; }

            /// <summary> A read-only list of member IDs currently in the lobby. </summary>
            IReadOnlyList<string> LobbyMembers { get; }

            /// <summary> Triggered when a lobby member is refreshed. </summary>
            event Action<string> OnLobbyMembersRefreshed;

            /// <summary> Initializes the lobby system and registers callbacks. </summary>
            void Initialize();

            /// <summary> Creates a new lobby. </summary>
            /// <param name="lobbyType">Platform-specific lobby type (e.g. ELobbyType).</param>
            /// <param name="onSuccess">Callback invoked on successful creation.</param>
            void CreateLobby(object lobbyType = null, Action onSuccess = null);

            /// <summary> Joins the lobby with the given ID. </summary>
            /// <param name="lobbyId">The ID of the lobby to join.</param>
            /// <param name="onJoinedLobby">Callback invoked once joined successfully.</param>
            void JoinLobby(string lobbyId, Action<string> onJoinedLobby = null);

            /// <summary> Leaves the currently joined lobby. </summary>
            void LeaveLobby();

            /// <summary> Gets all members currently in the lobby. </summary>
            List<string> GetAllLobbyMembers();
        }
    }

}
