using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;
using Random = UnityEngine.Random;


namespace AI
{
    public interface ICondition
    {
        public bool IsInverted { get; }

        bool Evaluate(Blackboard blackboard);
    }

    public abstract class ConditionBase : ICondition
    {
        public bool IsInverted { get; }

        public ConditionBase(bool isInverted = false)
        {
            IsInverted = isInverted;
        }

        public bool Evaluate(Blackboard blackboard)
        {
            bool result = EvaluateCondition(blackboard);
            return IsInverted ? !result : result;
        }

        protected abstract bool EvaluateCondition(Blackboard blackboard);
    }



    public class Blackboard
    {
        protected Dictionary<string, object> data;

        public Blackboard()
        {
            data = new Dictionary<string, object>();
        }

        public T GetData<T>(string key)
        {
            try
            {
                return (T)data[key];
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                return default;
            }
        }

        public void SetData<T>(string key, T value)
        {
            try
            {
                data[key] = value;
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }
    }

    public interface IState
    {
        public float Duration { get; } // Duration 0 is endless until transition
        public void Enter();
        public void Update();
        public void Exit();
    }

    public abstract class StateBase : IState
    {
        public float Duration { get; }

        protected Blackboard blackboard;

        public StateBase(Blackboard blackboard, float duration = 0)
        {
            this.blackboard = blackboard;
            Duration = duration;
        }

        public abstract void Enter();

        public abstract void Update();

        public abstract void Exit();
    }

    public abstract class DecisionMakerBase
    {
        protected Blackboard blackboard;

        public DecisionMakerBase(Blackboard blackboard)
        {
            this.blackboard = blackboard;
        }

        public abstract void Update(float delta);
    }

    public class StateTransition
    {
        protected ICondition[] conditions;
        protected IState toState;

        public StateTransition(IState toState, params ICondition[] conditions)
        {
            this.conditions = conditions;
            this.toState = toState;
        }

        public bool CanTransition(Blackboard blackboard)
        {
            if (conditions.Length == 0) // No conditions, always able to transition
                return true;

            foreach (ICondition condition in conditions)
            {
                if (condition.Evaluate(blackboard) == true)
                    return true;
            }
            return false;
        }

        public IState ToState => toState;
    }

    public class FSMDecisionMaker : DecisionMakerBase
    {
        private IState currentState;
        private float currentStateTimer;
        private Dictionary<IState, List<StateTransition>> transitions;

        public FSMDecisionMaker(Blackboard blackboard) : base(blackboard)
        {
            transitions = new Dictionary<IState, List<StateTransition>>();
        }

        public void AddTransition(IState fromState, IState toState, params ICondition[] conditions)
        {
            if (!transitions.ContainsKey(fromState))
                transitions[fromState] = new List<StateTransition>();

            transitions[fromState].Add(new StateTransition(toState, conditions));
        }

        public override void Update(float delta)
        {
            if (currentState == null) return;

            currentState.Update();
            currentStateTimer += delta;

            if (currentStateTimer < currentState.Duration)
                return;

            // Check transitions
            if (!transitions.ContainsKey(currentState))
                return;

            List<IState> validTransitionStates = new List<IState>();
            foreach (StateTransition transition in transitions[currentState])
            {
                if (transition.CanTransition(blackboard))
                    validTransitionStates.Add(transition.ToState);
            }
            if (validTransitionStates.Count == 0)
                return;

            // For now, randomly pick which of the valid transition is chosen
            ChangeState(validTransitionStates[Random.Range(0, validTransitionStates.Count)]);
        }

        public void ChangeState(IState toState)
        {
            Debug.Log($"State changed from {currentState} to {toState}");
            currentState?.Exit();
            currentState = toState;
            currentStateTimer = 0;
            toState.Enter();
        }
    }

    public class UtilityDecisionMaker : DecisionMakerBase
    {
        private List<(IState, Func<Blackboard, float>)> stateUtilities;

        public UtilityDecisionMaker(Blackboard blackboard) : base(blackboard)
        {
            stateUtilities = new List<(IState, Func<Blackboard, float>)>();
        }

        public void AddState(IState state, Func<Blackboard, float> utilityFunction)
        {
            stateUtilities.Add((state, utilityFunction));
        }

        public override void Update(float delta)
        {
            if (stateUtilities.Count == 0) return;

            // Evaluate utilities and pick the best state
            var bestState = stateUtilities
                .OrderByDescending(s => s.Item2(blackboard))
                .First().Item1;

            bestState.Update();
        }
    }
}