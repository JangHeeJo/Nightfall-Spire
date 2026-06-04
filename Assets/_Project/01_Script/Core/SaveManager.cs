using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;

// SaveData를 파일로 저장하고 불러오는 매니저입니다.
// tmp 파일과 backup 파일을 사용해 저장 중 앱이 종료되어도 원본 손상 가능성을 줄입니다.
public sealed class SaveManager
{
    private const string SaveFileName = "save_data.json"; // 저장 파일 이름
    private const string TempFileName = "save_data.tmp"; // 저장 중 사용할 임시 파일 이름
    private const string BackupFileName = "save_data.backup.json"; // 마지막 정상 저장 백업 파일 이름

    public SaveData CurrentSaveData { get; private set; } // 현재 로드된 저장 데이터

    private string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName); // 저장 파일 전체 경로
    private string TempFilePath => Path.Combine(Application.persistentDataPath, TempFileName); // 임시 파일 전체 경로
    private string BackupFilePath => Path.Combine(Application.persistentDataPath, BackupFileName); // 백업 파일 전체 경로

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
        CurrentSaveData = CreateSaveDataFromJson(json);

        if (CurrentSaveData == null)
        {
            CurrentSaveData = await TryLoadBackupAsync();
        }

        if (CurrentSaveData == null)
        {
            CurrentSaveData = SaveData.CreateDefault();
            await SaveAsync(CurrentSaveData);

            Debug.LogWarning("[SaveManager] 저장 데이터 복구에 실패해 기본 저장 데이터를 생성했습니다.");
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
        SaveJsonImmediate(json);

        Debug.Log("[SaveManager] 저장 데이터를 즉시 저장했습니다.");
    }

    // 전달받은 SaveData를 안전 저장 방식으로 파일에 기록합니다.
    private async UniTask SaveAsync(SaveData saveData)
    {
        string json = JsonUtility.ToJson(saveData, true);

        await File.WriteAllTextAsync(TempFilePath, json);
        ReplaceSaveFileWithTemp();

        Debug.Log($"[SaveManager] 저장 완료. Path: {SaveFilePath}");
    }

    // Json 문자열을 SaveData로 변환합니다. 실패하면 null을 반환합니다.
    private SaveData CreateSaveDataFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonUtility.FromJson<SaveData>(json);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[SaveManager] 저장 데이터 파싱 실패: {exception.Message}");
            return null;
        }
    }

    // 원본 저장 파일이 깨졌을 때 백업 저장 파일에서 복구를 시도합니다.
    private async UniTask<SaveData> TryLoadBackupAsync()
    {
        if (!File.Exists(BackupFilePath))
            return null;

        string backupJson = await File.ReadAllTextAsync(BackupFilePath);
        SaveData backupSaveData = CreateSaveDataFromJson(backupJson);

        if (backupSaveData != null)
            Debug.LogWarning("[SaveManager] 백업 저장 데이터를 사용해 복구했습니다.");

        return backupSaveData;
    }

    // 임시 파일 저장이 끝난 뒤 기존 저장 파일을 백업하고 교체합니다.
    private void ReplaceSaveFileWithTemp()
    {
        if (File.Exists(BackupFilePath))
            File.Delete(BackupFilePath);

        if (File.Exists(SaveFilePath))
            File.Move(SaveFilePath, BackupFilePath);

        File.Move(TempFilePath, SaveFilePath);
    }

    // 즉시 저장도 같은 교체 규칙을 사용합니다.
    private void SaveJsonImmediate(string json)
    {
        File.WriteAllText(TempFilePath, json);
        ReplaceSaveFileWithTemp();
    }
}
