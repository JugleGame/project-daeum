namespace Daeume.Core
{
    /// <summary>
    /// 자막 크기 3단계(spec-013)를 실제 배율로 바꾼다. HUD와 회상 자막이 같은 배율을 쓰도록
    /// 계산을 한곳에 모아 둔다 — 각자 다른 배율을 쓰면 화면마다 글자 크기 느낌이 달라진다.
    /// </summary>
    public static class SubtitleScale
    {
        public static float Resolve(int tier) => tier switch
        {
            0 => 0.8f,
            2 => 1.3f,
            _ => 1f
        };
    }
}
