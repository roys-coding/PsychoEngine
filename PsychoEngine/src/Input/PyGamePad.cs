using Microsoft.Xna.Framework.Input;
using GamePadXna = Microsoft.Xna.Framework.Input.GamePad;

namespace PsychoEngine.Input;

public class PyGamePad
{
    private readonly PlayerIndex  _playerIndex;
    private          GamePadState _previousState;
    private          GamePadState _currentState;

    public PyGamePad(PlayerIndex playerIndex)
    {
        _playerIndex = playerIndex;
    }

    public void Update(GameTime gameTime)
    {
        _previousState = _currentState;
        _currentState = GamePadXna.GetState(_playerIndex);
    }
}