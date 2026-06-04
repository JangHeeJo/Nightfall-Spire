using System;
using System.Collections.Generic;

// 광고, IAP, 분석 같은 외부 서비스를 등록하고 조회하는 순수 C# 등록소입니다.
// 실제 서비스 구현은 나중에 인터페이스 단위로 등록해서 GameRoot가 직접 무거워지지 않게 합니다.
public sealed class ServiceRegistry
{
    private readonly Dictionary<Type, object> services = new(); // 서비스 타입별 인스턴스 저장소

    // 같은 타입을 중복 등록하면 기존 값을 교체합니다.
    public void Register<TService>(TService service) where TService : class
    {
        if (service == null)
            throw new ArgumentNullException(nameof(service));

        services[typeof(TService)] = service;
    }

    // 등록된 서비스가 있으면 반환하고, 없으면 null을 반환합니다.
    public TService Get<TService>() where TService : class
    {
        return services.TryGetValue(typeof(TService), out object service)
            ? service as TService
            : null;
    }

    // 선택 서비스 조회용입니다. 등록 여부가 게임 흐름을 막지 않아야 할 때 사용합니다.
    public bool TryGet<TService>(out TService service) where TService : class
    {
        service = Get<TService>();
        return service != null;
    }
}
