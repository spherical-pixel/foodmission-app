using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using NUnit.Framework;

using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class HomePromptCoordinatorTests
    {
        private sealed class FakeHost : IHomePromptHost
        {
            public bool IsActive { get; set; } = true;
            public VisualElement DialogAnchor => null;
            public void RefreshActiveQuestWidget() { }
            public void NavigateToAuth() { }
        }

        private sealed class FakeGate : ICelebrationGate
        {
            public bool IsIdle { get; set; } = true;
            public event Action Idle;

            public void RaiseIdle()
            {
                IsIdle = true;
                Idle?.Invoke();
            }
        }

        private sealed class FakePrompt : IHomePrompt, IHomePromptInstance
        {
            private readonly string _name;
            private readonly List<string> _log;

            public FakePrompt(string name, HomePromptKind kind, List<string> log)
            {
                _name = name;
                Kind = kind;
                _log = log;
            }

            public HomePromptKind Kind { get; }
            public bool Pending { get; set; } = true;
            public HomePromptResult Result { get; set; } = HomePromptResult.Dismissed;
            public Action OnCheck { get; set; }
            public bool CheckThrows { get; set; }
            public bool ShowThrows { get; set; }

            public Task<IHomePromptInstance> CheckAsync()
            {
                _log.Add("check:" + _name);
                OnCheck?.Invoke();
                if (CheckThrows)
                {
                    throw new InvalidOperationException("check boom");
                }
                return Task.FromResult<IHomePromptInstance>(Pending ? this : null);
            }

            public Task<HomePromptResult> ShowAsync(IHomePromptHost host)
            {
                _log.Add("show:" + _name);
                if (ShowThrows)
                {
                    throw new InvalidOperationException("show boom");
                }
                return Task.FromResult(Result);
            }
        }

        private List<string> _log;
        private FakeHost _host;
        private FakeGate _gate;

        [SetUp]
        public void SetUp()
        {
            _log = new List<string>();
            _host = new FakeHost();
            _gate = new FakeGate();
        }

        private FakePrompt Prompt(string name, HomePromptKind kind) => new FakePrompt(name, kind, _log);

        private List<string> Shown() => _log.FindAll(l => l.StartsWith("show:"));

        [Test]
        public async Task RunsBlockingThenInfoThenAction_RegardlessOfListOrder()
        {
            var prompts = new IHomePrompt[] { Prompt("a", HomePromptKind.Action), Prompt("i", HomePromptKind.Info), Prompt("b", HomePromptKind.Blocking) };

            await new HomePromptCoordinator(prompts, _gate).RunAsync(_host);

            CollectionAssert.AreEqual(new[] { "show:b", "show:i", "show:a" }, Shown());
        }

        [Test]
        public async Task ShowsEveryInfoPrompt()
        {
            var prompts = new IHomePrompt[] { Prompt("i1", HomePromptKind.Info), Prompt("i2", HomePromptKind.Info), Prompt("i3", HomePromptKind.Info) };

            await new HomePromptCoordinator(prompts, _gate).RunAsync(_host);

            CollectionAssert.AreEqual(new[] { "show:i1", "show:i2", "show:i3" }, Shown());
        }

        [Test]
        public async Task DismissedAction_MovesOnToTheNextAction()
        {
            var prompts = new IHomePrompt[] { Prompt("a1", HomePromptKind.Action), Prompt("a2", HomePromptKind.Action) };

            await new HomePromptCoordinator(prompts, _gate).RunAsync(_host);

            CollectionAssert.AreEqual(new[] { "show:a1", "show:a2" }, Shown());
        }

        [Test]
        public async Task NavigatedAction_EndsTheRunWithoutCheckingTheRest()
        {
            FakePrompt first = Prompt("a1", HomePromptKind.Action);
            first.Result = HomePromptResult.Navigated;
            var prompts = new IHomePrompt[] { first, Prompt("a2", HomePromptKind.Action) };

            await new HomePromptCoordinator(prompts, _gate).RunAsync(_host);

            CollectionAssert.AreEqual(new[] { "check:a1", "show:a1" }, _log);
        }

        [Test]
        public async Task NavigatedBlocking_SkipsInfoAndActions()
        {
            FakePrompt legal = Prompt("b", HomePromptKind.Blocking);
            legal.Result = HomePromptResult.Navigated;
            var prompts = new IHomePrompt[] { legal, Prompt("i", HomePromptKind.Info), Prompt("a", HomePromptKind.Action) };

            await new HomePromptCoordinator(prompts, _gate).RunAsync(_host);

            CollectionAssert.AreEqual(new[] { "show:b" }, Shown());
        }

        [Test]
        public async Task NothingPending_IsSkipped()
        {
            FakePrompt empty = Prompt("i1", HomePromptKind.Info);
            empty.Pending = false;
            var prompts = new IHomePrompt[] { empty, Prompt("i2", HomePromptKind.Info) };

            await new HomePromptCoordinator(prompts, _gate).RunAsync(_host);

            CollectionAssert.AreEqual(new[] { "show:i2" }, Shown());
        }

        [Test]
        public async Task HostInactiveDuringCheck_ShowsNothing()
        {
            FakePrompt prompt = Prompt("i", HomePromptKind.Info);
            prompt.OnCheck = () => _host.IsActive = false; // the user left Home while the check awaited
            var prompts = new IHomePrompt[] { prompt, Prompt("a", HomePromptKind.Action) };

            await new HomePromptCoordinator(prompts, _gate).RunAsync(_host);

            CollectionAssert.AreEqual(new[] { "check:i" }, _log);
        }

        [Test]
        public async Task CheckThatThrows_IsLoggedAndTheRunContinues()
        {
            FakePrompt broken = Prompt("i1", HomePromptKind.Info);
            broken.CheckThrows = true;
            var prompts = new IHomePrompt[] { broken, Prompt("i2", HomePromptKind.Info) };
            LogAssert.Expect(LogType.Error, new Regex(@"\[HomePromptCoordinator\].*check boom"));

            await new HomePromptCoordinator(prompts, _gate).RunAsync(_host);

            CollectionAssert.AreEqual(new[] { "show:i2" }, Shown());
        }

        [Test]
        public async Task ShowThatThrows_CountsAsDismissed()
        {
            FakePrompt broken = Prompt("a1", HomePromptKind.Action);
            broken.ShowThrows = true;
            var prompts = new IHomePrompt[] { broken, Prompt("a2", HomePromptKind.Action) };
            LogAssert.Expect(LogType.Error, new Regex(@"\[HomePromptCoordinator\].*show boom"));

            await new HomePromptCoordinator(prompts, _gate).RunAsync(_host);

            CollectionAssert.AreEqual(new[] { "show:a1", "show:a2" }, Shown());
        }

        [Test]
        public async Task WaitsForCelebrationGate_BeforeShowing()
        {
            _gate.IsIdle = false;
            var prompts = new IHomePrompt[] { Prompt("i", HomePromptKind.Info) };

            Task run = new HomePromptCoordinator(prompts, _gate).RunAsync(_host);
            Assert.AreEqual(0, Shown().Count, "a celebration from another screen is still showing");

            _gate.RaiseIdle();
            await run;

            CollectionAssert.AreEqual(new[] { "show:i" }, Shown());
        }

        [Test]
        public async Task WaitsAGapBetweenShownPrompts()
        {
            var prompts = new IHomePrompt[] { Prompt("i1", HomePromptKind.Info), Prompt("i2", HomePromptKind.Info) };
            var clock = System.Diagnostics.Stopwatch.StartNew();

            await new HomePromptCoordinator(prompts, _gate, promptGap: TimeSpan.FromMilliseconds(200)).RunAsync(_host);

            Assert.GreaterOrEqual(clock.ElapsedMilliseconds, 180, "the next dialog never opens inside the previous one's close");
            CollectionAssert.AreEqual(new[] { "show:i1", "show:i2" }, Shown());
        }

        [Test]
        public async Task GateTimeout_ShowsAnyway()
        {
            _gate.IsIdle = false;
            var prompts = new IHomePrompt[] { Prompt("i", HomePromptKind.Info) };

            await new HomePromptCoordinator(prompts, _gate, TimeSpan.FromMilliseconds(20)).RunAsync(_host);

            CollectionAssert.AreEqual(new[] { "show:i" }, Shown());
        }
    }
}
