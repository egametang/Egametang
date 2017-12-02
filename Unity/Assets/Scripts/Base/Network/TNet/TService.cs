﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Model
{
	public sealed class TService: AService
	{
		private TcpListener acceptor;

		private readonly Dictionary<long, TChannel> idChannels = new Dictionary<long, TChannel>();

		private readonly EQueue<Action> actions = new EQueue<Action>();
		
		/// <summary>
		/// 即可做client也可做server
		/// </summary>
		public TService(IPEndPoint ipEndPoint)
		{
			this.acceptor = new TcpListener(ipEndPoint);
			this.acceptor.Start();
		}

		public TService()
		{
		}

		public override void Dispose()
		{
			if (this.acceptor == null)
			{
				return;
			}

			foreach (long id in this.idChannels.Keys.ToArray())
			{
				TChannel channel = this.idChannels[id];
				channel.Dispose();
			}
			this.acceptor.Stop();
			this.acceptor = null;
		}

		public override void Add(Action action)
		{
			this.actions.Enqueue(action);
		}

		public override AChannel GetChannel(long id)
		{
			TChannel channel = null;
			this.idChannels.TryGetValue(id, out channel);
			return channel;
		}

		public override async Task<AChannel> AcceptChannel()
		{
			if (this.acceptor == null)
			{
				throw new Exception("service construct must use host and port param");
			}
			TcpClient tcpClient = await this.acceptor.AcceptTcpClientAsync();
			TChannel channel = new TChannel(tcpClient, this);
			this.idChannels[channel.Id] = channel;
			return channel;
		}

		public override AChannel ConnectChannel(IPEndPoint ipEndPoint)
		{
			TcpClient tcpClient = new TcpClient();
			TChannel channel = new TChannel(tcpClient, ipEndPoint, this);
			this.idChannels[channel.Id] = channel;

			return channel;
		}


		public override void Remove(long id)
		{
			TChannel channel;
			if (!this.idChannels.TryGetValue(id, out channel))
			{
				return;
			}
			if (channel == null)
			{
				return;
			}
			this.idChannels.Remove(id);
			channel.Dispose();
		}
		
		public override void Update()
		{
			while (this.actions.Count > 0)
			{
				Action action = this.actions.Dequeue();
				action();
			}
		}
	}
}