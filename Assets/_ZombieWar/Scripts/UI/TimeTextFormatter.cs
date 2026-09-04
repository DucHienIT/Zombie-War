using TMPro;

namespace ZombieWar.UI
{
    public sealed class TimeTextFormatter
    {
        private readonly char[] _buffer = new char[5];

        public void Apply(TMP_Text target, int totalSeconds)
        {
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            _buffer[0] = (char)('0' + minutes / 10 % 10);
            _buffer[1] = (char)('0' + minutes % 10);
            _buffer[2] = ':';
            _buffer[3] = (char)('0' + seconds / 10);
            _buffer[4] = (char)('0' + seconds % 10);
            target.SetCharArray(_buffer, 0, _buffer.Length);
        }
    }
}
