﻿using Newtonsoft.Json.Linq;
using WampSharp.Core.Client;
using WampSharp.Core.Contracts.V1;
using WampSharp.Core.Listener;
using WampSharp.Core.Proxy;
using WampSharp.Core.Serialization;
using WampSharp.Fleck;
using WampSharp.PubSub.Client;
using WampSharp.Rpc;

namespace WampSharp.Api
{
    public class WampChannelFactory : WampChannelFactory<JToken>
    {
        public WampChannelFactory() : this(new JsonFormatter())
        {
        }

        public WampChannelFactory(IWampFormatter<JToken> formatter) : base(formatter)
        {
        }
    }

    public class WampChannelFactory<TMessage> : IWampChannelFactory<TMessage>
    {
        private readonly IWampFormatter<TMessage> mFormatter;
        private readonly IWampRpcClientFactory<TMessage> mRpcClientFactory;
        private readonly IWampPubSubClientFactory<TMessage> mPubSubClientFactory;
        private readonly WampServerProxyBuilder<TMessage, IWampClient<TMessage>, IWampServer> mServerProxyBuilder;

        public WampChannelFactory(IWampFormatter<TMessage> formatter)
        {
            mFormatter = formatter;

            mRpcClientFactory =
                GetRpcClientFactory();

            mPubSubClientFactory =
                GetPubSubClientFactory();

            mServerProxyBuilder =
                GetServerProxyBuilder<IWampClient<TMessage>>();
        }

        private IWampPubSubClientFactory<TMessage> GetPubSubClientFactory()
        {
            WampServerProxyBuilder<TMessage, IWampPubSubClient<TMessage>, IWampServer> proxyBuilder = 
                GetServerProxyBuilder<IWampPubSubClient<TMessage>>();

            PubSub.Client.WampServerProxyFactory<TMessage> serverProxyFactory =
                new PubSub.Client.WampServerProxyFactory<TMessage>(proxyBuilder);

            WampPubSubClientFactory<TMessage> result =
                new WampPubSubClientFactory<TMessage>(serverProxyFactory,
                                                      mFormatter);

            return result;
        }

        private WampRpcClientFactory<TMessage> GetRpcClientFactory()
        {
            WampRpcSerializer rpcSerializer = 
                new WampRpcSerializer(new WampRpcMethodAttributeProcUriMapper());

            WampServerProxyBuilder<TMessage, IWampRpcClient<TMessage>, IWampServer> serverProxyBuilder = 
                GetServerProxyBuilder<IWampRpcClient<TMessage>>();

            Rpc.WampServerProxyFactory<TMessage> serverProxyFactory =
                new Rpc.WampServerProxyFactory<TMessage>(serverProxyBuilder);
            
            WampRpcClientHandlerBuilder<TMessage> clientHandlerBuilder = 
                new WampRpcClientHandlerBuilder<TMessage>(mFormatter, serverProxyFactory);
            
            WampRpcClientFactory<TMessage> result = 
                new WampRpcClientFactory<TMessage>
                    (rpcSerializer,
                     clientHandlerBuilder);

            return result;
        }

        private WampServerProxyBuilder<TMessage, TRawClient, IWampServer> GetServerProxyBuilder<TRawClient>()
        {
            WampOutgoingRequestSerializer<TMessage> outgoingRequestSerializer = 
                new WampOutgoingRequestSerializer<TMessage>(mFormatter);

            WampServerProxyIncomingMessageHandlerBuilder<TMessage, TRawClient> incomingHandlerBuilder = 
                new WampServerProxyIncomingMessageHandlerBuilder<TMessage, TRawClient>(mFormatter);
            
            WampServerProxyOutgoingMessageHandlerBuilder<TMessage, TRawClient> outgoingHandlerBuilder = 
                new WampServerProxyOutgoingMessageHandlerBuilder<TMessage, TRawClient>(incomingHandlerBuilder);

            return new WampServerProxyBuilder<TMessage, TRawClient, IWampServer>
                (outgoingRequestSerializer, outgoingHandlerBuilder);
        }

        public IWampChannel<TMessage> CreateChannel(IWampConnection<TMessage> connection)
        {
            return new WampChannel<TMessage>(connection,
                                             mRpcClientFactory,
                                             mPubSubClientFactory,
                                             mServerProxyBuilder);
        }
    }
}