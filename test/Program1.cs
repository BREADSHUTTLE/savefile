using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*
 * 정렬된 두 배열을 합치기
 *    - MergeSortedArray 함수를 구현하시면 됩니다.
 *    - array1과 array2는 이미 오름차순으로 정렬되어 있습니다.
 *    - 합쳐진 배열도 오름차순으로 정렬되어야 합니다.
 */
class Program
{
	static void MergeSortedArray(int[] array1, int[] array2, int[] destArray)
	{
		// destArray의 크기는 array1과 array2의 크기를 합친 것으로 이미 생성되어 있음
		int i = 0;
		int j = 0;
		int k = 0;

		while (i < array1.Length && j < array2.Length)
		{
			if (array1[i] <= array2[j])
				destArray[k++] = array1[i++];
			else
				destArray[k++] = array2[j++];
		}

		while (i < array1.Length)
			destArray[k++] = array1[i++];

		while (j < array2.Length)
			destArray[k++] = array2[j++];
	}
}
