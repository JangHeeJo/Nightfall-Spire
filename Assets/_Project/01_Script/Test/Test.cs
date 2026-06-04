using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

// list 개념
// 런타임 중 동적으로 크기가 변하는 동적 배열이다
// 내부적으로는 


public class Test : MonoBehaviour
{
    private List<int> testList = new List<int>();
    int capacity = 0;
    void Start()
    {
        capacity = testList.Capacity;
        Debug.Log(capacity);
        CheckCapacity();
    }
    public void CheckCapacity()
    {
        for (int i = 0; i < 100; i++) 
        {
            testList.Add(i);
            if (capacity != testList.Capacity)
            {
                capacity = testList.Capacity;
                Debug.Log(capacity);
            }
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
