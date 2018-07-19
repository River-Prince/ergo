using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ergo
{
	public class HttpProxy : TcpProxy
	{
		private const int CONNECTION_ENDED_WAIT = 30000;

		protected override void ProcessClientConnection(TcpClient client)
		{
			NetworkStream clientStream = client.GetStream();
			StreamReader clientReader = new StreamReader(clientStream);
			StreamWriter clientWriter = new StreamWriter(clientStream);

			TcpClient server = null;
			NetworkStream serverStream = null;
			StreamReader serverReader = null;
			StreamWriter serverWriter = null;

			try
			{
				// Get the first line of the HTTP request which will tell us what to do
				// and where to go. We can use stream reader here because the first request
				// will always be UTF8.
				string request = clientReader.ReadLine();

				ParseRequestDetails(request, out string method, out string host, out int port, out string httpVersion);

				ConnectToRemote(
					host,
					port,
					out server,
					out serverStream,
					out serverReader,
					out serverWriter);

				if (method == "CONNECT")
				{
					// If its a connect request this doesn't need to be sent to the remote
					// so just read it off the stream.
					while (String.IsNullOrEmpty(clientReader.ReadLine()) == false) ;

					SendConnectResponse(clientWriter, httpVersion);
				}
				else
				{
					// If we aren't using CONNECT then we have already read the first line
					// that we want to send to the server. We cant just write it alone as the 
					// server while reject it. We need to send the entire header all at once.
					serverWriter.WriteLine(request);

					string line;
					while (String.IsNullOrEmpty(line = clientReader.ReadLine()) == false)
					{
						serverWriter.WriteLine(line);
					}

					serverWriter.WriteLine();

					serverWriter.Flush();
				}

				// Tunnels streams into each other sync. This is basically a transparent proxy.
				// When both are done then the connections have been closed.

				Task clientToServerTask = clientStream.CopyToAsync(serverStream);
				Task serverToClientTask = serverStream.CopyToAsync(clientStream);

				Task.WaitAny(
					clientToServerTask,
					serverToClientTask);

				Task.WaitAll(
					new Task[] { clientToServerTask, serverToClientTask },
					CONNECTION_ENDED_WAIT);
			}
			finally
			{
				try
				{
					DisposeConnection(client, clientStream, clientReader, clientWriter);
					DisposeConnection(server, serverStream, serverReader, serverWriter);
				}
				catch
				{
				}
			}
		}

		/// <summary>
		/// Parses the first line of a HTTP request to get the action and address info
		/// </summary>
		private void ParseRequestDetails(
			string request,
			out string method,
			out string host,
			out int port,
			out string httpVersion)
		{
			string[] requestParts = request.Split(' ');

			method = requestParts[0];
			httpVersion = requestParts[2];

			Uri uri = new Uri(requestParts[1]);

			// CONNECT doesnt send the scheme, because it isnt needed.
			// But Uri cant parse URLs without a scheme, so we just add one.
			if (method == "CONNECT")
			{
				uri = new Uri("http://" + requestParts[1]);
			}

			host = uri.Host;
			port = uri.Port;
		}

		private void SendConnectResponse(StreamWriter writer, string httpVersion)
		{
			writer.WriteLine(httpVersion + " 200 Connection established");
			writer.WriteLine();
			writer.Flush();
		}
	}
}
