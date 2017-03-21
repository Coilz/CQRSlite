﻿using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CQRSlite.Cache;
using CQRSlite.Tests.Substitutes;
using Xunit;

namespace CQRSlite.Tests.Cache
{
    public class When_evicting_cache_entry
    {
        private CacheRepository _rep;
        private TestAggregate _aggregate;
        private ICache _cache;
        private ConcurrentDictionary<Guid, ManualResetEvent> _locks;

        public When_evicting_cache_entry()
        {
            _cache = new MemoryCache();
            _rep = new CacheRepository(new TestRepository(), new TestEventStore(), _cache);
            _aggregate = _rep.Get<TestAggregate>(Guid.NewGuid()).Result;
            var field = _rep.GetType().GetField("_locks", BindingFlags.Static | BindingFlags.NonPublic);
            _locks = (ConcurrentDictionary<Guid, ManualResetEvent>)field.GetValue(_rep);
            _locks.TryGetValue(_aggregate.Id, out var resetEvent);
            _cache.Remove(_aggregate.Id);
            resetEvent.WaitOne(500);
        }

        [Fact]
        public void Should_remove_lock()
        {
            _locks.TryGetValue(_aggregate.Id, out var val);
            Assert.Null(val);
        }

        [Fact]
        public void Should_not_throw_if_no_lock()
        {
            _locks.Clear();
            _cache.Remove(_aggregate.Id);
        }

        [Fact]
        public async Task Should_get_new_aggregate_next_get()
        {
            _cache.Remove(_aggregate.Id);

            var aggregate = await _rep.Get<TestAggregate>(_aggregate.Id);
            Assert.NotEqual(_aggregate, aggregate);
        }
    }
}
