﻿
using System;

namespace rlc
{

    public static class Utility
    {

        public static void Rotate<T>(this T[] array, int count) // Source: https://stackoverflow.com/questions/38482696/how-to-efficiently-rotate-an-array (I'm surprised there is no standard rotate algorithm)
        {
            if (array == null || array.Length < 2) return;
            count %= array.Length;
            if (count == 0) return;
            int left = count < 0 ? -count : array.Length + count;
            int right = count > 0 ? count : array.Length - count;
            if (left <= right)
            {
                for (int i = 0; i < left; i++)
                {
                    var temp = array[0];
                    Array.Copy(array, 1, array, 0, array.Length - 1);
                    array[array.Length - 1] = temp;
                }
            }
            else
            {
                for (int i = 0; i < right; i++)
                {
                    var temp = array[array.Length - 1];
                    Array.Copy(array, 0, array, 1, array.Length - 1);
                    array[0] = temp;
                }
            }
        }
    }

}