﻿using System.Threading;
using System.Collections.Generic;
using System;


namespace SimpleWebsocketServer
{

        

    public class ChannelBridge
    {
        private Dictionary<string, List<IObserver<string>>> channels;
        public string adminPassword;
        public Thread managementThread;
        public CancellationTokenSource ManagementCancelationToken;

        public ChannelBridge(string adminPassword)
        {
            this.adminPassword = adminPassword;
            this.channels = new Dictionary<string, List<IObserver<string>>>();
            managementThread = Thread.CurrentThread;
        }

        public IDisposable Subscribe(ChannelSubscriber channelSubscriber, string channel)
        {
            List<IObserver<string>> observers = GetOrCreate(channels, channel);

            if (!observers.Contains(channelSubscriber))
            {
                observers.Add(channelSubscriber);
            }
            return new Unsubscriber<string>(observers, channelSubscriber);
        }

        public void PublishContent(string channel, string content)
        {
            List<IObserver<string>> observers = GetOrCreate(channels, channel);

            foreach (var observer in observers)
                observer.OnNext(content);
        }

        public TValue GetOrCreate<TKey, TValue>(IDictionary<TKey, TValue> dict, TKey key) where TValue : new()
        {
            if (!dict.TryGetValue(key, out TValue val))
            {
                val = new TValue();
                dict.Add(key, val);
            }

            return val;
        }

    }
}
