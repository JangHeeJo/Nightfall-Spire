using NUnit.Framework;
using UnityEngine;

// 팝업 요청과 결과 타입의 기본 계약을 검증합니다.
public sealed class PopupRequestTests
{
    // 팝업 요청은 Key, 프리팹, 열기 정책, 딤 사용 여부를 그대로 보관해야 합니다.
    [Test]
    public void Constructor_StoresRequestValues()
    {
        GameObject popupObject = new GameObject("TestPopup");
        BasePopup popup = popupObject.AddComponent<BasePopup>();

        PopupRequest<BasePopup> request = new PopupRequest<BasePopup>(
            "notice",
            popup,
            PopupOpenPolicy.SingleInstance,
            PopupPriority.Important,
            false);

        Assert.That(request.Key, Is.EqualTo("notice"));
        Assert.That(request.Prefab, Is.EqualTo(popup));
        Assert.That(request.OpenPolicy, Is.EqualTo(PopupOpenPolicy.SingleInstance));
        Assert.That(request.Priority, Is.EqualTo(PopupPriority.Important));
        Assert.That(request.UseDim, Is.False);

        Object.DestroyImmediate(popupObject);
    }

    // 팝업 Key가 비어 있거나 프리팹이 없으면 요청을 만들 수 없어야 합니다.
    [Test]
    public void Constructor_RejectsInvalidValues()
    {
        Assert.That(() => new PopupRequest<BasePopup>("", null), Throws.ArgumentException);
        Assert.That(() => new PopupRequest<BasePopup>("notice", null), Throws.ArgumentNullException);
    }

    // 팝업 결과는 확인/취소 여부와 선택 결과 Payload를 호출자에게 전달해야 합니다.
    [Test]
    public void Result_ExposesCloseReasonAndPayload()
    {
        PopupResult confirmed = new PopupResult(PopupCloseReason.Confirmed, 1001);
        PopupResult cancelled = new PopupResult(PopupCloseReason.Cancelled);

        Assert.That(confirmed.IsConfirmed, Is.True);
        Assert.That(confirmed.IsCancelled, Is.False);
        Assert.That(confirmed.Payload, Is.EqualTo(1001));

        Assert.That(cancelled.IsConfirmed, Is.False);
        Assert.That(cancelled.IsCancelled, Is.True);
    }
}
