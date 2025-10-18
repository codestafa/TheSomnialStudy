namespace AI.Base
{
    public interface IState
    {
        void Enter(AiContext context);
        void Update(AiContext context);
        void Exit(AiContext context);
    }
}
