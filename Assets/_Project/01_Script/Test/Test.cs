using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.iOS;

// list 개념
// 런타임 중 동적으로 크기가 변하는 동적 배열이다
// 내부적으로는 배열로 이루어져 있음
// capacity -> 미리 확보해둔 공간
// count -> 실제 정보가 담겨있는 개수
// add 시간 복잡도 O(1) capacity 늘어나면 o(n)
// 바로 찾는건 보통 o(1)  순회 o(n) for 문 o(n2)
// 추가할게 많다면 add 보단 addrange 가 성능이 좋음(시간복잡도 o(m) -> capacity 늘어나면 시간 복잡도 o(n+m)  ) capacity 늘어나면 시간 복잡도 o(n) 
// insert -> 평균 o(n) remove, removeat 다 비슷한 성능
// removeall -> o(n) 전체 순회
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
    public void CheckCapacity() // 미리 할당하면 capacity 도 늘어남
    {
        List<int> a = new(); 
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
    public class MyList<T>
    {
        public T[] arr = new T[0];
        public int count = 0;

        public int capacity => arr.Length;
        public int Count => count;

        public void Add(T value)
        {
            if(capacity == count)
            {
                SetCapacity();
            }
            arr[count] = value;
            count++;
        }
        public bool Remove(T value)
        {
           for(int i = 0; i < count; i++)
           {
                if(EqualityComparer<T>.Default.Equals(arr[i], value))
                {
                    RemoveAt(i);
                    return true;
                }
           }
            return false;
        }
        public void RemoveAt(int index)
        {  
            for (int i = index; i < count - 1; i++)
            {
                arr[i] = arr[i + 1];
            }
            count--;
            arr[count] = default;
        }
        public void SetCapacity()
        {
            int newCapacity = capacity == 0 ? 4 : capacity * 2;
            T[] newArr = new T[newCapacity];

            for(int  i = 0; i < arr.Length; i++)
            {
                newArr[i] = arr[i];
            }
            arr = newArr;
        }
        public void Clear()
        {
            for (int i = 0; i < count; i++)
            {
                arr[i] = default;
            }
            count = 0;
        }
        public void Reverse()
        {
            int left = 0;
            int right = count - 1;

            while(left < right)
            {
                T temp = arr[left];
                arr[left] = arr[right];
                arr[right] = temp;

                left++;
                right--;

            }
        }
    }
    //public class MyList
    //{
    //    public int _count => arr.Length - 1;
    //    public int _capacity;
    //    public int?[] arr = new int?[0];


    //    public void SetCapacity(int cap)
    //    {
    //        _capacity = cap;
    //    }
    //    //public void SetCapacity()
    //    //{
    //    //   int newCapacity = _capacity == 0 ? 4 : _capacity * 2;
    //    //   int[] newArr = new int[newCapacity];
    //    //    for (int i = 0; i < arr.Length; i++)
    //    //    {
    //    //        newArr[i] = arr[i];
    //    //    }

    //    //    arr = newArr;
    //    //    _capacity = newCapacity;
    //    //}
    //    public void Add(int value)
    //    {
    //        // -------------------- 캐파 꽉참 add 안될경우 resize
    //        // -- resize시 원래 cap x2 배
    //        if(_capacity == _count)
    //        {
    //            SetCapacity(_capacity*2);
    //        }
    //        arr[_count] = value;
    //    }

    //    public void Remove(int value)
    //    {
    //        // arr 에서  value 찾아서 삭제
    //        for (int i = 0; i < arr.Length; i++)
    //        {
    //            if (arr[i] == value)
    //            {
    //                arr[i] = null;
    //            }
    //        }
    //        for (int i = 0; i < arr.Length; i++)
    //        {
    //            if (arr[i] == null)
    //            {
    //                for (int j = i + 1; j < arr.Length; j++)
    //                {
    //                    arr[i] = arr[j];
    //                }
    //                break;
    //            }
    //        }
    //    }

    //    public void RemoveAt(int idx)
    //    {

    //        if(_count > idx)
    //        {
    //            if (arr[idx] != null)
    //            {
    //                arr[idx] = null;
    //            }
    //            for(int i = idx; i < arr.Length; i++)
    //            {
    //                for (int j = i + 1; j < arr.Length; j++)
    //                {
    //                    arr[i] = arr[j];
    //                }
    //                break;
    //            }
    //        }
    //        // arr idx remove
            

    //    }

    //    public void Clear() 
    //    {
    //        for(int i = 0; i < arr.Length; i++)
    //        {
    //            arr[i] = null;                
    //        }
    //        _capacity = 0;
    //    }

    //    public void Reverse()
    //    {
    //        // 리스트 리버스

    //        // 뒤집기
    //        int?[] arrTemp = arr;

    //        for(int i = arrTemp.Length; i > 0; i--)
    //        {
    //            for(int j = 0; j < arr.Length; i ++)
    //            {
    //                arr[j] = arrTemp[i];
    //            }
    //        }
    //    }
    //}

    public class TestClass
    {
        public List<int> Swap(List<int> list)
        {
            // 여기서 2번째 5번째 로그한번 찍고
            // 2번쨰 5번쨰 순서 스왑
            // 바꾸고 바뀐값 로그찍고

            return list;
        }
    }



    //public class MakeT<T>
    //{
    //    private T[] arr = new T[0];
    //    private int count = 0;
    //    private int capacity = 0;

    //    public int Count => count;
    //    public int Capacity => capacity;

    //    public void Add(T value)
    //    {
    //        if(count == capacity)
    //        {
    //            SetCapacity();
    //        }
    //        arr[count] = value;
    //        count++;
    //    }
    //    public void Remove(int value)
    //    {
    //       for(int i = 0; i < arr.Length; i++) 
    //       {
    //            if (arr[i] == value)
    //            {

    //            }
    //       }
    //    }


    //    private void SetCapacity()
    //    {
    //        int newCapacity = capacity == 0? 4 : capacity*2;
    //        T[] newArr = new T[newCapacity];
    //        count = arr.Length;
    //        for(int i = 0; i< arr.Length; i++)
    //        {
    //            newArr[i] = arr[i];
    //        }

    //        arr = newArr;
    //        capacity = newCapacity;
    //    }
    //}
    


}
