using System.IO;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

// SaveData를 파일로 저장하고 불러오는 매니저입니다.
// 현재는 Json 파일 저장으로 시작하고, 나중에 암호화나 클라우드 저장으로 확장할 수 있습니다.
public sealed class SaveManager
{
    private const string SaveFileName = "save_data.json"; // 저장 파일 이름

    public SaveData CurrentSaveData { get; private set; } // 현재 로드된 저장 데이터

    // 저장 파일 전체 경로입니다.
    private string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    // 저장 데이터를 비동기로 불러옵니다.
    public async UniTask<SaveData> LoadAsync()
    {
        if (!File.Exists(SaveFilePath))
        {
            CurrentSaveData = SaveData.CreateDefault();
            await SaveAsync(CurrentSaveData);

            Debug.Log("[SaveManager] 저장 파일이 없어 기본 저장 데이터를 생성했습니다.");
            return CurrentSaveData;
        }

        string json = await File.ReadAllTextAsync(SaveFilePath);

        if (string.IsNullOrWhiteSpace(json))
        {
            CurrentSaveData = SaveData.CreateDefault();
            await SaveAsync(CurrentSaveData);

            Debug.LogWarning("[SaveManager] 저장 파일이 비어 있어 기본 저장 데이터를 생성했습니다.");
            return CurrentSaveData;
        }

        CurrentSaveData = JsonUtility.FromJson<SaveData>(json);

        if (CurrentSaveData == null)
        {
            CurrentSaveData = SaveData.CreateDefault();
            await SaveAsync(CurrentSaveData);

            Debug.LogWarning("[SaveManager] 저장 데이터 파싱에 실패해 기본 저장 데이터를 생성했습니다.");
        }

        Debug.Log("[SaveManager] 저장 데이터를 불러왔습니다.");

        return CurrentSaveData;
    }

    // 현재 저장 데이터를 비동기로 저장합니다.
    public UniTask SaveCurrentAsync()
    {
        if (CurrentSaveData == null)
        {
            Debug.LogWarning("[SaveManager] CurrentSaveData가 null이라 저장을 건너뜁니다.");
            return UniTask.CompletedTask;
        }

        return SaveAsync(CurrentSaveData);
    }

    // 앱 종료 시점처럼 비동기를 기다리기 애매한 상황에서 즉시 저장합니다.
    public void SaveCurrentImmediate()
    {
        if (CurrentSaveData == null)
            return;

        string json = JsonUtility.ToJson(CurrentSaveData, true);
        File.WriteAllText(SaveFilePath, json);

        Debug.Log("[SaveManager] 저장 데이터를 즉시 저장했습니다.");
    }

    // 전달받은 SaveData를 Json 파일로 저장합니다.
    private async UniTask SaveAsync(SaveData saveData)
    {
        string json = JsonUtility.ToJson(saveData, true);

        await File.WriteAllTextAsync(SaveFilePath, json);

        Debug.Log($"[SaveManager] 저장 완료. Path: {SaveFilePath}");
    }
}