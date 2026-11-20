using System.Collections;
using Framework.Sequencing;
using NUnit.Framework;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FrameworkTestScene.Tests
{
    public class TestSceneSequenceTests
    {
        private const string TestSceneGuid = "389ad195432444d9eb2bc275941721ee";

        private SequentialTask _sequence;

        [UnitySetUp]
        public IEnumerator LoadTestScene()
        {
#if UNITY_EDITOR
            var testScenePath = AssetDatabase.GUIDToAssetPath(TestSceneGuid);
            Assert.That(testScenePath, Is.Not.Empty, "Import the sequencer samples first: Tools > Framework > Import sequencer samples.");
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(testScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
#else
            Assert.Ignore("The Test scene is only loaded from the editor.");
            yield break;
#endif

            _sequence = Object.FindFirstObjectByType<SequentialTask>();
            Assert.IsNotNull(_sequence, "The Test scene has no SequentialTask.");
            _sequence.Cancel();
        }

        [UnityTearDown]
        public IEnumerator CancelTheSequenceBeforeTheSceneIsUnloaded()
        {
            if (_sequence != null)
            {
                _sequence.Cancel();
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator Skipping_every_task_walks_the_whole_scene_sequence_to_completion()
        {
            yield return RunPastIntro();

            yield return SkipUntilFinished();

            Assert.AreEqual(SequenceState.Completed, _sequence.Runner.State);
        }

        [UnityTest]
        public IEnumerator Pausing_freezes_the_current_task_and_resuming_continues_it()
        {
            yield return RunPastIntro();
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
            yield return RunPastIntro();
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
            yield return RunPastIntro();
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
            yield return RunPastIntro();

            _sequence.Cancel();
            yield return null;
            Assert.AreEqual(SequenceState.Cancelled, _sequence.Runner.State);

            yield return RunPastIntro();
            Assert.AreEqual(SequenceState.Running, _sequence.Runner.State);
            Assert.AreEqual(0, _sequence.Runner.CurrentIndex);
        }

        private IEnumerator RunPastIntro()
        {
            _sequence.Run();
            yield return null;
            if (_sequence.IsRunning && _sequence.CurrentStageIndex < 0)
            {
                _sequence.SkipCurrentStage();
                yield return null;
            }
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
