using System;
using System.Collections.Generic;

namespace Daeume.Core
{
    /// <summary>
    /// 시스템끼리 서로를 직접 알지 않고 "소식"만 주고받게 해 주는 중계소다.
    ///
    /// 왜 필요한가(유니티 처음이라면):
    /// 보통은 A 스크립트가 B 스크립트를 변수로 들고 직접 함수를 부른다. 그러면 A는 B를 알아야 하고,
    /// B가 바뀌면 A도 깨진다. 이 프로젝트는 3명이 A(시스템)/B(추격·레벨)/C(연출) 영역을 나눠 만들기 때문에
    /// 서로의 내부를 모르는 상태로 붙여야 한다. 그래서 "체력이 바뀌었다" 같은 사실만 여기에 던지고(Publish),
    /// 관심 있는 쪽이 알아서 받아 간다(Subscribe).
    ///
    /// 설계 적합성 검토:
    /// - 메시지 종류를 타입(제네릭 T)으로 구분하므로 문자열 오타로 인한 사고가 없다. 적합하다.
    /// - Dictionary(사전)의 키가 "메시지 타입", 값이 "그 타입을 받기로 한 함수 묶음"이다.
    /// - 주의: 구독을 해제(Unsubscribe)하지 않고 오브젝트가 파괴되면 죽은 함수가 남아 예외가 난다.
    ///   그래서 이 프로젝트의 모든 구독자는 OnDisable/OnDestroy에서 반드시 해제한다.
    /// </summary>
    public sealed class EventBus
    {
        // 키: 메시지 타입(예: PlayerHealthChanged), 값: 그 메시지를 받기로 등록한 함수들의 묶음.
        // Delegate는 "함수를 담는 변수"라고 생각하면 된다. 여러 개를 하나로 합칠 수 있다.
        private readonly Dictionary<Type, Delegate> handlers = new();

        /// <summary>
        /// T 타입 메시지를 받겠다고 등록한다. 예: Subscribe&lt;PlayerHealthChanged&gt;(OnHealth)
        /// </summary>
        public void Subscribe<T>(Action<T> handler)
        {
            if (handler == null)
            {
                // null을 등록하면 나중에 호출 시점에 원인을 알 수 없는 오류가 난다.
                // 그래서 "지금 여기서" 명확히 터뜨린다. 디버깅 비용을 크게 줄여 주므로 적합하다.
                throw new ArgumentNullException(nameof(handler));
            }

            var eventType = typeof(T);
            handlers.TryGetValue(eventType, out var existing);
            // Delegate.Combine: 기존에 등록된 함수 묶음 뒤에 새 함수를 이어 붙인다.
            handlers[eventType] = Delegate.Combine(existing, handler);
        }

        /// <summary>
        /// 등록을 취소한다. 오브젝트가 사라지기 전에 반드시 불러야 죽은 참조가 남지 않는다.
        /// </summary>
        public void Unsubscribe<T>(Action<T> handler)
        {
            if (handler == null)
            {
                // 해제는 "없으면 그만"이므로 예외 대신 조용히 무시한다. 종료 순서가 뒤엉켜도 안전하다.
                return;
            }

            var eventType = typeof(T);
            if (!handlers.TryGetValue(eventType, out var existing))
            {
                return;
            }

            var remaining = Delegate.Remove(existing, handler);
            if (remaining == null)
            {
                // 마지막 구독자까지 빠졌으면 사전에서 키 자체를 지운다. 빈 껍데기를 남기지 않는다.
                handlers.Remove(eventType);
                return;
            }

            handlers[eventType] = remaining;
        }

        /// <summary>
        /// T 타입 메시지를 지금 등록돼 있는 모든 구독자에게 전달한다.
        /// </summary>
        public void Publish<T>(T message)
        {
            if (handlers.TryGetValue(typeof(T), out var existing))
            {
                // 합쳐진 함수 묶음을 한 번 호출하면 등록 순서대로 전부 실행된다.
                // 검토 메모: 구독자 하나가 예외를 던지면 그 뒤 구독자는 실행되지 않는다.
                // 지금 규모(구독자 수 개)에서는 오히려 오류를 빨리 드러내는 편이 낫다고 판단해 그대로 둔다.
                // 구독자가 늘어 한쪽 실패가 전체를 막는 일이 생기면 그때 개별 try/catch로 감싼다.
                ((Action<T>)existing).Invoke(message);
            }
        }

        /// <summary>
        /// 모든 구독을 비운다. 게임을 껐다 켜는(플레이 모드 종료) 시점에 GameManager가 호출한다.
        /// </summary>
        public void Clear()
        {
            handlers.Clear();
        }
    }
}
