using EFT.InputSystem;

namespace DynamicMaps.Utils
{
    internal class MiniMapPositionInputNode : InputNode
    {
        public override ETranslateResult TranslateCommand(ECommand command)
        {
            if (command == ECommand.ToggleAlternativeShooting || command == ECommand.EndAlternativeShooting)
                return ETranslateResult.Ignore;

            return GetDefaultBlockResult(command);
        }

        public override void TranslateAxes(ref float[] axes)
        {
            axes = null;
        }

        public override ECursorResult ShouldLockCursor()
        {
            return ECursorResult.ShowCursor;
        }
    }
}
