using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONI_MP.Networking
{
    /// <summary>
    /// A recreation of steam networking types
    /// <para />
    /// Reference: https://partner.steamgames.com/doc/api/steamnetworkingtypes
    /// </summary>
    [Flags]
    public enum SteamNetworkingSend
    {
        /// <summary>
        /// Send the message unreliably. Can be lost. Messages <c>*can*</c> be larger than a single MTU (UDP packet), but there is no retransmission, so if any piece of the message is lost, the entire message will be dropped.
        /// <para/>
        /// The sending API does have some knowledge of the underlying connection, so if there is no NAT-traversal accomplished or there is a recognized adjustment happening on the connection, the packet will be batched until the connection is open again.
        /// <para/>
        /// Migration note: This is not exactly the same as k_EP2PSendUnreliable! You probably want k_ESteamNetworkingSendType_UnreliableNoNagle
        /// </summary>
        Unreliable = 0,

        /// <summary>
        /// Disable Nagle's algorithm.
        /// <para/>
        /// By default, Nagle's algorithm is applied to all outbound messages. This means that the message will NOT be sent immediately, in case further messages are sent soon after you send this, which can be grouped together. Any time there is enough buffered data to fill a packet, the packets will be pushed out immediately, but partially-full packets not be sent until the Nagle timer expires. See ISteamNetworkingSockets::FlushMessagesOnConnection, ISteamNetworkingMessages::FlushMessagesToUser
        /// <para/>
        /// NOTE: Don't just send every message without Nagle because you want packets to "get there quicker". Make sure you understand the problem that Nagle is solving before disabling it. If you are sending small messages, often many at the same time, then it is very likely that it will be more efficient to leave Nagle enabled. A typical proper use of this flag is when you are sending what you know will be the last message sent for a while (e.g. the last in the server simulation tick to a particular client), and you use this flag to flush all messages.
        /// </summary>
        NoNagle = 1,

        /// <summary>
        /// Send a message unreliably, bypassing Nagle's algorithm for this message and any messages currently pending on the Nagle timer.
        /// <para/>
        /// This is equivalent to using k_ESteamNetworkingSend_Unreliable and then immediately flushing the messages using ISteamNetworkingSockets::FlushMessagesOnConnection or ISteamNetworkingMessages::FlushMessagesToUser.
        /// <para/>
        /// (But using this flag is more efficient since you only make one API call.)
        /// </summary>
        UnreliableNoNagle = Unreliable | NoNagle,

        /// <summary>
        /// If the message cannot be sent very soon (because the connection is still doing some initial handshaking, route negotiations, etc), then just drop it. 
        /// <para />
        /// This is only applicable for unreliable messages. 
        /// <para/>
        /// Using this flag on reliable messages is invalid.
        /// </summary>
        NoDelay = 4,

        /// <summary>
        /// Send an unreliable message, but if it cannot be sent relatively quickly, just drop it instead of queuing it. This is useful for messages that are not useful if they are excessively delayed, such as voice data.
        /// <para/>
        /// A message will be dropped under the following circumstances:
        /// <list type="bullet">
        /// <item>
        /// the connection is not fully connected. (E.g. the "Connecting" or "FindingRoute" states)
        /// </item>
        /// <item>
        /// there is a sufficiently large number of messages queued up already such that the current message will not be placed on the wire in the next ~200ms or so.
        /// </item>
        /// </list>
        /// <para/>
        /// If a message is dropped for these reasons, k_EResultIgnored will be returned.
        /// <para/>
        /// NOTE: The Nagle algorithm is not used, and if the message is not dropped, any messages waiting on the Nagle timer are immediately flushed.
        /// </summary>
        UnreliableNoDelay = Unreliable | NoDelay | NoNagle,

        /// <summary>
        /// Reliable message send. Can send up to 512 * 1024 bytes in a single message. Does fragmentation/re-assembly of messages under the hood, as well as a sliding window for efficient sends of large chunks of data.
        /// <para/>
        /// The Nagle algorithm is used.
        /// <para/>
        /// Migration note: This is NOT the same as k_EP2PSendReliable, it's more like k_EP2PSendReliableWithBuffering
        /// </summary>
        Reliable = 8,

        /// <summary>
        /// Send a message reliably, but bypass Nagle's algorithm.
        /// <para/>
        /// Migration note: This is equivalent to k_EP2PSendReliable
        /// </summary>
        ReliableNoNagle = Reliable | NoNagle
    }
}
