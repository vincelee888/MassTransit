using System;
using MassTransit.ServiceBus.Subscriptions.Messages;

namespace MassTransit.ServiceBus.SubscriptionsManager
{
    

    public class SubscriptionServiceBus : ServiceBus
    {
        private ISubscriptionRepository _repository;


        public SubscriptionServiceBus(IEndpoint endpoint, ISubscriptionStorage subscriptionStorage, ISubscriptionRepository repository)
            : base(endpoint, subscriptionStorage)
        {
            _repository = repository;
            this.Subscribe<SubscriptionChange>(OnSubscriptionMessageReceived);
            this.Subscribe<RequestCacheUpdate>(OnRequestCacheUpdate);
            this.Subscribe<RequestCacheUpdateForMessage>(OnRequestSubscribersForMessage);
        }


        public void OnSubscriptionMessageReceived(MessageContext<SubscriptionChange> ctx)
        {
            RegisterForUpdates(ctx.Envelope);

            // Add / Remove Subscription to Repository
            switch(ctx.Message.ChangeType)
            {
                case SubscriptionChange.SubscriptionChangeType.Add:
                    _repository.Add(SubscriptionMapper.MapFrom(ctx.Message));
                    break;
                case SubscriptionChange.SubscriptionChangeType.Remove:
                    _repository.Deactivate(SubscriptionMapper.MapFrom(ctx.Message));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Publish it so others get it?
            this.Publish(ctx.Message);
        }

        public void OnRequestCacheUpdate(MessageContext<RequestCacheUpdate> ctx)
        {
            RegisterForUpdates(ctx.Envelope);

            //return a complete list of SubscriptionMessages
            ctx.Reply(new CacheUpdateResponse(SubscriptionMapper.MapFrom(_repository.List())));
        }


        public void OnRequestSubscribersForMessage(MessageContext<RequestCacheUpdateForMessage> ctx)
        {
            RegisterForUpdates(ctx.Envelope);

            //return a complete list of SubscriptionMessages
            ctx.Reply(new CacheUpdateResponse(SubscriptionMapper.MapFrom(_repository.List(ctx.Message.Message))));
        }

        public void RegisterForUpdates(IEnvelope env)
        {
            //This is basically setting anybody that talks to us up for updates
            this._repository.Add(new Subscription(env.ReturnEndpoint.Uri.AbsolutePath, typeof(CacheUpdateResponse).FullName));
        }
    }
}