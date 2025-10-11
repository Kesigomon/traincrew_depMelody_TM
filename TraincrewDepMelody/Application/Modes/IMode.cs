namespace TraincrewDepMelody.Application.Modes
{
    /// <summary>
    /// モードインターフェース
    /// </summary>
    public interface IMode
    {
        void OnEnter();
        void OnExit();
        void OnButtonPressed();
        void OnButtonReleased();
        void Update();
    }
}
