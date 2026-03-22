using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using ONI_MP.DebugTools;
using ONI_MP.Menus;
using ONI_MP.Networking.Components;

namespace ONI_MP.Networking.Transfer
{
	public static class TcpFileTransferClient
	{
		public static void Download(string hostIp, int tcpPort, ulong clientId, Action<string, byte[]> onComplete, Action<string> onError)
		{
			Thread thread = new Thread(() => DownloadThread(hostIp, tcpPort, clientId, onComplete, onError))
			{
				IsBackground = true,
				Name = "TcpFileTransfer_Download"
			};
			thread.Start();
		}

		private static void DownloadThread(string hostIp, int tcpPort, ulong clientId, Action<string, byte[]> onComplete, Action<string> onError)
		{
			try
			{
				using (TcpClient client = new TcpClient())
				{
					client.ReceiveBufferSize = 65536;
					IAsyncResult ar = client.BeginConnect(hostIp, tcpPort, null, null);
					if (!ar.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(10)))
					{
						throw new TimeoutException("TCP connection timed out");
					}
					client.EndConnect(ar);

					NetworkStream stream = client.GetStream();
					stream.ReadTimeout = 30000;

					byte[] idBytes = BitConverter.GetBytes(clientId);
					stream.Write(idBytes, 0, 8);
					stream.Flush();

					byte[] fileNameLenBuf = ReadExact(stream, 4);
					int fileNameLen = BitConverter.ToInt32(fileNameLenBuf, 0);
					byte[] fileNameBuf = ReadExact(stream, fileNameLen);
					string fileName = Encoding.UTF8.GetString(fileNameBuf);

					byte[] fileSizeBuf = ReadExact(stream, 4);
					int fileSize = BitConverter.ToInt32(fileSizeBuf, 0);

					DebugConsole.Log($"[TcpFileTransfer] Downloading '{fileName}' ({fileSize} bytes)");

					byte[] data = new byte[fileSize];
					int received = 0;
					int lastReportedStep = -1;

					while (received < fileSize)
					{
						int toRead = Math.Min(65536, fileSize - received);
						int n = stream.Read(data, received, toRead);
						if (n == 0)
							throw new IOException("Connection closed during transfer");
						received += n;

						int step = (int)((long)received * 10 / fileSize);
						if (step != lastReportedStep)
						{
							lastReportedStep = step;
							int percent = step * 10;
							MainThreadExecutor.dispatcher.QueueEvent(() =>
							{
								MultiplayerOverlay.Show($"Downloading save... {percent}%");
							});
						}
					}

					DebugConsole.Log($"[TcpFileTransfer] Download complete: '{fileName}' ({received} bytes)");

					MainThreadExecutor.dispatcher.QueueEvent(() =>
					{
						onComplete(fileName, data);
					});
				}
			}
			catch (Exception ex)
			{
				DebugConsole.LogError($"[TcpFileTransfer] Download failed: {ex.Message}");
				MainThreadExecutor.dispatcher.QueueEvent(() =>
				{
					onError(ex.Message);
				});
			}
		}

		private static byte[] ReadExact(NetworkStream stream, int count)
		{
			byte[] buf = new byte[count];
			int read = 0;
			while (read < count)
			{
				int n = stream.Read(buf, read, count - read);
				if (n == 0)
					throw new IOException("Connection closed while reading");
				read += n;
			}
			return buf;
		}
	}
}
