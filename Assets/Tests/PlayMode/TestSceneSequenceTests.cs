using System.Collections;
using Framework.Sequencing;
using NUnit.Framework;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FrameworkTestScene.Tests
{
    public class TestSceneSequenceTests
    {
        private const string TestScenePath = "Assets/Framework Package/Samples/Scenes/Test scene.unity";

        private SequentialTask _sequence;

        [UnitySetUp]
        public IEnumerator LoadTestScene()
        {
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(TestScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
#else
            Assert.Ignore("The Test scene is only loaded from the editor.");
            yield break;
#endif

            _sequence = Object.FindFirstObjectByType<SequentialTask>();
            Assert.IsNotNull(_sequence, "The Test scene has no SequentialTask.");
            _sequence.Cancel();
        }

        [UnityTest]
        public IEnumerator Skipping_every_task_walks_the_whole_scene_sequence_to_completion()
        {
            _sequence.Run();
            yield return null;

            yield return SkipUntilFinished();

            Assert.AreEqual(SequenceState.Completed, _sequence.Runner.State);
        }

        [UnityTest]
        public IEnumerator Pausing_freezes_the_current_task_and_resuming_continues_it()
        {
            _sequence.Run();
            yield return null;
            var taskWhenPaused = _sequence.CurrentTask;

            _sequence.Pause();
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(SequenceState.Paused, _sequence.Runner.State);
            Assert.AreSame(taskWhenPaused, _sequence.CurrentTask);

            _sequence.Resume();
            Assert.AreEqual(SequenceState.Running, _sequence.Runner.State);
        }

        [UnityTest]
        public IEnumerator Going_back_returns_to_the_previous_task_and_restart_returns_to_the_first()
        {
            _sequence.Run();
            yield return null;
            var firstTask = _sequence.CurrentTask;

            _sequence.SkipCurrentTask();
            yield return null;
            Assert.AreNotSame(firstTask, _sequence.CurrentTask);

            _sequence.GoBack();
            yield return null;
            Assert.AreSame(firstTask, _sequence.CurrentTask);

            _sequence.SkipCurrentTask();
            yield return null;
            _sequence.Restart();
            yield return null;
            Assert.AreSame(firstTask, _sequence.CurrentTask);
        }

        [UnityTest]
        public IEnumerator Restarting_a_finished_scene_sequence_lets_it_finish_again()
        {
            _sequence.Run();
            yield return null;
            yield return SkipUntilFinished();
            Assert.AreEqual(SequenceState.Completed, _sequence.Runner.State);

            _sequence.Restart();
            yield return null;
            Assert.AreEqual(0, _sequence.Runner.CurrentIndex);

            yield return SkipUntilFinished();
            Assert.AreEqual(SequenceState.Completed, _sequence.Runner.State);
        }

        [UnityTest]
        public IEnumerator Cancelling_stops_the_sequence_and_a_fresh_run_starts_over()
        {
            _sequence.Run();
            yield return null;

            _sequence.Cancel();
            yield return null;
            Assert.AreEqual(SequenceState.Cancelled, _sequence.Runner.State);

            _sequence.Run();
            yield return null;
            Assert.AreEqual(SequenceState.Running, _sequence.Runner.State);
            Assert.AreEqual(0, _sequence.Runner.CurrentIndex);
        }

        private IEnumerator SkipUntilFinished()
        {
            for (var attempt = 0; attempt < _sequence.TaskList.Count * 2 && _sequence.IsRunning; attempt++)
            {
                _sequence.SkipCurrentTask();
                yield return null;
            }
        }
    }
}
