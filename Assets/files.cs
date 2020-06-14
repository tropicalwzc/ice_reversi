using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

public class files
{

    int cannotopen = 0;
    string filePath;
    string fileName;
    public string getpathandname(string fileName)
    {
        filePath = Path.Combine(Application.persistentDataPath, fileName);
        return filePath;
    }
    public void WriteTextFile(string fileName, string stringData)
    {
        string pathAndName = getpathandname(fileName);
        FileInfo textFile = new FileInfo(pathAndName);
        if (textFile.Exists)
            textFile.Delete();
        StreamWriter writer;
        writer = textFile.CreateText();
        writer.Write(stringData);

        writer.Close();
    }


    public string ReadTextFile(string fileName = "temper.txt")
    {
        string dataAsString = "0000";
        string pathAndName = getpathandname(fileName);
        try
        {
            StreamReader textReader = File.OpenText(pathAndName);
            dataAsString = textReader.ReadToEnd();
            textReader.Close();
        }
        catch (Exception)// can not read or find the file
        {
            WriteTextFile(fileName, "0000");
        }

        return dataAsString;
    }
}
