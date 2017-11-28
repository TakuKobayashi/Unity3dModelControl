using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Runtime.InteropServices;

public class Util
{
    public static T FindCompomentInChildren<T>(Transform root) where T : class
    {
        for (int i = 0; i < root.childCount; ++i)
        {
            Transform t = root.GetChild(i);
            T compoment = t.GetComponent<T>();
            if (!compoment.Equals(null))
            {
                return compoment;
            }

            // GetCompomentのnullとreturn nullとは違うらしい...
            T childCompoment = FindCompomentInChildren<T>(t);
            if (childCompoment != null)
            {
                return childCompoment;
            }
        }

        return null;
    }

    public static List<T> FindAllCompomentInChildren<T>(Transform root) where T : class
    {
        List<T> compoments = new List<T>(); 
        for (int i = 0; i < root.childCount; ++i)
        {
            Transform t = root.GetChild(i);
            T compoment = t.GetComponent<T>();
            if (!compoment.Equals(null))
            {
                compoments.Add(compoment);
            }

            // GetCompomentのnullとreturn nullとは違うらしい...
            List<T> childCompoments = FindAllCompomentInChildren<T>(t);
            compoments.AddRange(childCompoments);
        }

        return compoments;
    }
}