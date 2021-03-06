﻿using HouseofCat.Logger;
using HouseofCat.RabbitMQ;
using HouseofCat.Utilities.Errors;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace HouseofCat.RabbitMQ.Workflows
{
    public class ConsumerBlock<TOut> : IPropagatorBlock<TOut, TOut>
    {
        public Task Completion { get; private set; }

        private readonly ILogger<ConsumerBlock<TOut>> _logger;
        private readonly IConsumer<TOut> _consumer;
        private ITargetBlock<TOut> _bufferBlock;
        private ISourceBlock<TOut> _sourceBufferBlock;

        public ConsumerBlock(IConsumer<TOut> consumer)
        {
            Guard.AgainstNull(consumer, nameof(consumer));
            _logger = LogHelper.LoggerFactory.CreateLogger<ConsumerBlock<TOut>>();
            _consumer = consumer;

            _bufferBlock = new BufferBlock<TOut>();
            _sourceBufferBlock = (ISourceBlock<TOut>)_bufferBlock;
            Completion = _bufferBlock.Completion;
        }

        public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, TOut messageValue, ISourceBlock<TOut> source, bool consumeToAccept)
        {
            return _bufferBlock.OfferMessage(messageHeader, messageValue, source, consumeToAccept);
        }

        public void Complete()
        {
            _bufferBlock.Complete();
        }

        public void Fault(Exception exception)
        {
            _bufferBlock.Fault(exception);
        }

        public IDisposable LinkTo(ITargetBlock<TOut> target, DataflowLinkOptions linkOptions)
        {
            return _sourceBufferBlock.LinkTo(target, linkOptions);
        }

        public TOut ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOut> target, out bool messageConsumed)
        {
            throw new NotImplementedException();
        }

        public void ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<TOut> target)
        {
            throw new NotImplementedException();
        }

        public bool ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOut> target)
        {
            throw new NotImplementedException();
        }

        public async Task StartConsumerAsync()
        {
            await _consumer.StartConsumerAsync();
            await Task.Run(() => StreamToBufferAsync());
        }

        // TODO: And TaskCompletionSource w/ Token
        // TODO: Store runner in Task;
        private async Task StreamToBufferAsync(CancellationToken token = default)
        {
            try
            {
                await foreach(var message in _consumer.GetConsumerBuffer().ReadAllAsync(token).ConfigureAwait(false))
                {
                    await _bufferBlock.SendAsync(message);
                }
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception)
            {

            }
        }
    }
}
