// BootScene 로딩 화면 View 계약입니다.
// Controller는 Unity UI 컴포넌트를 직접 모르고 이 계약만 통해 진행률과 문구를 갱신합니다.
public interface IBootLoadingView
{
    void SetProgress(float progress01); // 0~1 진행률을 표시합니다.
    void SetStatusText(string statusText); // 현재 로딩 단계를 표시합니다.
    void SetVersionText(string versionText); // 앱 버전이나 빌드 정보를 표시합니다.
}
