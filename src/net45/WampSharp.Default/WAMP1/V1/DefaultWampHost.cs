﻿using Newtonsoft.Json.Linq;
using WampSharp.Core.Listener;
using WampSharp.Core.Serialization;
using WampSharp.Fleck;
using WampSharp.Newtonsoft;
using WampSharp.V2.Binding.Parsers;

namespace WampSharp.V1
{
    public class DefaultWampHost : DefaultWampHost<JToken>
    {
        public DefaultWampHost(string location) :
            base(location, new JTokenMessageParser(), new JsonFormatter())
        {
        }
    }

    public class DefaultWampHost<TMessage> : WampHost<TMessage>
    {
        public DefaultWampHost(string location,
                               IWampTextMessageParser<TMessage> parser,
                               IWampFormatter<TMessage> formatter) :
                                   this(new FleckWampConnectionListener<TMessage>(location, new Wamp1Binding<TMessage>(parser, formatter)), formatter)
        {
        }

        public DefaultWampHost(IWampConnectionListener<TMessage> connectionListener, IWampFormatter<TMessage> formatter)
            : base(connectionListener, formatter)
        {
        }
    }
}