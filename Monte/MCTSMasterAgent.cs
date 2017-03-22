namespace Monte
{
    public abstract class MCTSMasterAgent : AIAgent
    {
        protected MCTSMasterAgent(double _thinkingTime, double _exploreWeight, int _maxRollout) : base(_thinkingTime,
            _exploreWeight, _maxRollout){}

        protected MCTSMasterAgent() : base(){}

        protected MCTSMasterAgent (string fileName): base(fileName){ }
        //Rollout function (plays random moves till it hits a termination)
        protected abstract void rollout(AIState rolloutStart);
    }
}