using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ergo
{
    public abstract class TcpProxy
    {
		private IList<TcpListener> _listeners = new List<TcpListener>();

		public IList<IPEndPoint> ListenerAddresses { get; } = new List<IPEndPoint>();

		public void Start()
		{
			if (ListenerAddresses.Count > 0)
			{
				_listeners.Clear();

				foreach (IPEndPoint address in ListenerAddresses)
				{
					TcpListener listener = new TcpListener(address);

					listener.Start();

					listener.BeginAcceptTcpClient(OnAcceptConnection, listener);

					_listeners.Add(listener);
				}
			}
		}

		public void Stop()
		{
			foreach (TcpListener listener in _listeners)
			{
				listener.Stop();
			}
		}

		private void OnAcceptConnection(IAsyncResult asyncResult)
		{
			TcpListener listener = (TcpListener)asyncResult.AsyncState;

			try
			{
				TcpClient client = listener.EndAcceptTcpClient(asyncResult);

				Task.Run(() => { ProcessClientConnection(client); });

			}
			catch (ObjectDisposedException)
			{
				return;
			}

			// Start the call to accept another connection
			if (listener != null)
			{
				listener.BeginAcceptTcpClient(OnAcceptConnection, listener);
			}
		}

		protected void ConnectToRemote(
			string host,
			int port,
			out TcpClient server,
			out NetworkStream stream,
			out StreamReader reader,
			out StreamWriter writer)
		{
			server = new TcpClient();
			server.Connect(host, port);

			stream = server.GetStream();
			reader = new StreamReader(stream);
			writer = new StreamWriter(stream);
		}

		protected void DisposeConnection(
			TcpClient client,
			NetworkStream stream,
			StreamReader reader,
			StreamWriter writer)
		{
			if (writer != null)
			{
				writer.Close();
				writer.Dispose();
			}

			if (reader != null)
			{
				reader.Close();
				reader.Dispose();
			}

			if (stream != null)
			{
				stream.Close();
				stream.Dispose();
			}

			if (client != null)
			{
				client.Close();
			}
		}

		protected abstract void ProcessClientConnection(TcpClient client);
	}
}
