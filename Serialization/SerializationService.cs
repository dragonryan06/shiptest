using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using Newtonsoft.Json;

namespace ShipTest.Serialization;

public class SerializationService
{
    public static bool WriteObjectToFile<T>(string filePath, T objectToWrite) where T : new()
    {
        bool result;
        TextWriter writer = null;
        try
        {
            var jsonString = JsonConvert.SerializeObject(objectToWrite);
            writer = new StreamWriter(filePath);
            writer.Write(jsonString);
            result = true;
        }
        catch (Exception e)
        {
            result = false;
            GD.PrintErr(e.ToString());
        }
        finally
        {
            writer?.Close();
        }

        return result;
    }

    public static bool ReadObjectFromFile<T>(string filePath, out T objectRead) where T : new()
    {
        bool result;
        TextReader reader = null;
        objectRead = default;
        try
        {
            reader = new StreamReader(filePath);
            var fileContents = reader.ReadToEnd();
            objectRead = JsonConvert.DeserializeObject<T>(fileContents);
            result = true;
        }
        catch (Exception e)
        {
            result = false;
            GD.PrintErr(e.ToString());
        }
        finally
        {
            reader?.Close();
        }

        return result;
    }
}