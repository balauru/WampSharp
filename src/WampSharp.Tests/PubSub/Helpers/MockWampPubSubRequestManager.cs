﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using WampSharp.Core.Contracts.V1;

namespace WampSharp.Tests.PubSub.Helpers
{
    public class MockWampPubSubRequestManager<TMessage>
    {
        private readonly ICollection<WampPublishRequest<TMessage>> mPublications = new List<WampPublishRequest<TMessage>>();
        private readonly ICollection<WampSubscribeRequest<TMessage>> mSubscriptions = new List<WampSubscribeRequest<TMessage>>();
        private readonly ICollection<WampSubscribeRequest<TMessage>> mSubscriptionRemovals = new List<WampSubscribeRequest<TMessage>>();

        public ICollection<WampSubscribeRequest<TMessage>> SubscriptionRemovals
        {
            get { return mSubscriptionRemovals; }
        }

        public ICollection<WampSubscribeRequest<TMessage>> Subscriptions
        {
            get { return mSubscriptions; }
        }

        public ICollection<WampPublishRequest<TMessage>> Publications
        {
            get { return mPublications; }
        }

        public IWampServer GetServer(IWampPubSubClient<TMessage> client)
        {
            return new MockWampPubSubServerProxy(this, client);
        }

        private class MockWampPubSubServerProxy : IWampServer
        {
            private readonly MockWampPubSubRequestManager<TMessage> mParent;
            private readonly IWampPubSubClient<TMessage> mClient;

            public MockWampPubSubServerProxy(MockWampPubSubRequestManager<TMessage> parent, IWampPubSubClient<TMessage> client)
            {
                mParent = parent;
                mClient = client;
            }

            public void Prefix(IWampClient client, string prefix, string uri)
            {
            }

            public void Call(IWampClient client, string callId, string procUri, params object[] arguments)
            {
            }

            public void Subscribe(IWampClient client, string topicUri)
            {
                mParent.mSubscriptions.Add(new WampSubscribeRequest<TMessage>()
                {
                    Client = mClient,
                    TopicUri = topicUri
                });
            }

            public void Unsubscribe(IWampClient client, string topicUri)
            {
                mParent.mSubscriptionRemovals.Add(new WampSubscribeRequest<TMessage>()
                {
                    Client = mClient,
                    TopicUri = topicUri
                });
            }

            public void Publish(IWampClient client, string topicUri, object @event)
            {
                InnerPublish(client, topicUri, @event);
            }

            public void Publish(IWampClient client, string topicUri, object @event, bool excludeMe)
            {
                InnerPublish(client, topicUri, @event, excludeMe);
            }

            public void Publish(IWampClient client, string topicUri, object @event, string[] exclude)
            {
                InnerPublish(client, topicUri, @event, null, exclude);
            }

            public void Publish(IWampClient client, string topicUri, object @event, string[] exclude, string[] eligible)
            {
                InnerPublish(client, topicUri, @event, null, exclude, eligible);
            }

            private void InnerPublish(IWampClient client, string topicUri, object @event, bool? excludeMe = null, string[] exclude = null, string[] eligible = null)
            {
                mParent.mPublications.Add(new WampPublishRequest<TMessage>()
                {
                    Client = mClient,
                    Eligible = eligible,
                    Event = @event,
                    ExcludeMe = excludeMe,
                    Exclude = exclude,
                    TopicUri = topicUri
                });
            }

        }
    }
}